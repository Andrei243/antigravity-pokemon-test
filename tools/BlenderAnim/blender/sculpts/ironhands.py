# Iron Hands sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ironhands.py -- <the export's Iron Hands.glb> <out.glb>
# Its worked example is Hariyama (hariyama.py), whose shapes it does not share: built anew. From the references: a
# robot sumo. A small grey head sunk into the top of the torso under a navy cap that folds forward to a point, a dark
# face under its brim with two small lit eyes, a grey bolt at each temple; a great white-grey barrel of a torso with a
# dark hollow in its front, a round grille in the hollow's top and a white bar curving under it like a U; a yellow
# round light on each upper side; yellow blocks of upper arms at the torso's sides, and at their ends the design's
# largest masses, two enormous hands each a V of two thick fingers on a palm, grey on the back and yellow on the palm;
# two huge navy haunches under the torso, the dark crotch between them, short dark legs on black feet with a yellow
# band. (The task said "blue-lit palms": the pictures' palms are yellow with white specks, and the cap is navy.)
# The art holds the hands up high; here they are carried forward and down before the hips, palms forward, the one
# allowed exception to arms inside the body's width (the hands are the design's largest masses).
# Reference ratios (3D picture, the cap to the feet, hands left out): head with cap / height 0.22; torso width /
# height 0.54; haunches across / height 0.94; a hand's span / height 0.7.
# Bones: body (0, 0, 0.4); head (0, -0.02, 0.6); arms (+-0.18, 0, 0.53); legs (+-0.12, 0, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, lathe, ring_band, srgb, inside, unit, boxy, eye_look, polygon,
                       outward, args)

GREY, WHITE, NAVY, YELLOW = "#C6CAD2", "#E8EAEE", "#2C3E80", "#F8D63C"
DARK, BLACK, GRILLE = "#4A4C56", "#24242C", "#36383F"
FACE, EYE = srgb("#202634"), srgb("#F6E46A")

T = np.array([0.0, 0.0, 0.47])               # the torso
TR = np.array([0.215, 0.19, 0.2])
H = np.array([0.0, -0.04, 0.64])             # the head
HR = np.array([0.085, 0.08, 0.08])


def add_body(k):
    def barrel(V):
        V = boxy(2.6)(V)
        V[:, 0] *= 1 + 0.08 * np.clip(-V[:, 2], 0, 1)
        return V
    k.add("torso", ell(T, TR, deform=barrel), WHITE, "body")
    k.add("shoulder", ell((0.19, 0.0, 0.5), (0.085, 0.085, 0.085)), WHITE, "body", mirror=True)
    # the dark hollow in the torso's front and the crotch below it
    k.add("belly", ell((0, -0.08, 0.34), (0.12, 0.12, 0.13)), DARK, "body")
    # the hollow's arch: one patch cast from the front, crisp at its edge
    arch = [(-1, -1), (1, -1), (1, 0.2), (0.8, 0.62), (0.45, 0.9), (0, 1), (-0.45, 0.9), (-0.8, 0.62), (-1, 0.2)]
    k.decal((0, -0.19, 0.4), (0, -1, 0), (0.125, 0.11), DARK, eps=0.003, outline=polygon(arch), rings=12, segs=72)
    # the round grille in the hollow's top: a darker disc in a ring
    def grille(u, v):
        c = np.tile(srgb(GRILLE), (len(u), 1))
        c[np.hypot(u, v) > 0.78] = srgb("#62656E")
        return c
    k.decal((0, -0.19, 0.44), (0, -1, 0), (0.062, 0.062), grille, eps=0.0055, rings=12, segs=56)
    # the white bar curving under the grille like a U, ends in the torso's sides
    pts = [(0.15, -0.15, 0.44), (0.12, -0.205, 0.36), (0.0, -0.228, 0.32), (-0.12, -0.205, 0.36), (-0.15, -0.15, 0.44)]
    k.add("bar", outward(tube(pts, [0.024] * 5)), WHITE, "body", fuse=False, surface="metal")
    # the round yellow lights on the upper sides
    for s in (1, -1):
        k.decal((0.2 * s, -0.06, 0.56), unit(0.95 * s, -0.25, 0.1), (0.028, 0.028), YELLOW, eps=0.003, rings=10,
                segs=40)
    # the navy haunches, huge and round, the hips joining them under the torso
    k.add("hips", ell((0, 0.01, 0.26), (0.22, 0.19, 0.13)), NAVY, "body")
    k.add("haunch", ell((0.165, 0.0, 0.205), (0.165, 0.17, 0.165)), NAVY, "leg{s}", mirror=True)
    # the dark crotch between the haunches
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.06, 0.004) * inside(P[:, 2] - 0.3, 0.004) * (P[:, 1] < 0.05),
            DARK, on=["hips", "haunch", "belly"])
    # short dark legs, black feet with a yellow band
    k.add("shin", outward(tube([(0.14, -0.01, 0.07), (0.145, -0.02, 0.035)], [0.048, 0.044])), DARK, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("foot", outward(ell((0.145, -0.05, 0.026), (0.07, 0.1, 0.034), deform=boxy(2.4))), BLACK, "leg{s}",
          fuse=False, mirror=True, surface="metal")
    k.add("footband", ring_band((0.145, -0.05, 0.036), (0, 0, 1), lambda v: 0.0 * v + 0.068, 0.014, thick=0.006),
          YELLOW, "leg{s}", fuse=False, mirror=True, surface="metal")


def add_head(k):
    k.add("head", ell(H, HR), GREY, "head")
    # the navy cap folding forward to a point, crisp at its rim
    def cap(V):
        V = V.copy()
        V[:, 2] = np.clip(V[:, 2], -0.35, 1)
        f = np.clip(V[:, 2] + 0.35, 0, 1.35) / 1.35
        V[:, 1] -= 0.45 * f ** 2
        return V
    k.add("cap", outward(ell(H + (0, 0.008, 0.06), (0.098, 0.094, 0.07), deform=cap)), NAVY, "head", fuse=False,
          surface="cloth")
    k.add("knot", outward(ell(H + (0, -0.07, 0.115), (0.03, 0.03, 0.028))), NAVY, "head", fuse=False,
          surface="cloth")
    # the bolts at the temples
    for s in (1, -1):
        k.add(f"bolt{s}", lathe(H + (0.078 * s, 0.0, -0.01), (s, 0, 0), [(0, 0), (0.03, 0), (0.03, 0.03), (0, 0.032)]),
              GREY, "head", fuse=False, surface="metal")
    # the dark face under the brim
    k.decal(H + (0, -HR[1] * 0.92, -0.012), unit(0, -1, -0.05), (0.068, 0.048), FACE, eps=0.003,
            outline=polygon([(-1, 1), (1, 1), (1, -0.3), (0.6, -1), (-0.6, -1), (-1, -0.3)]), rings=12, segs=56)


def add_arm(k, s):
    side = "R" if s > 0 else "L"
    q = np.array([s, 1, 1.0])
    SH, EL = np.array((0.2, -0.02, 0.45)) * q, np.array((0.27, -0.13, 0.37)) * q
    # yellow blocks of the upper arm and forearm, a grey cuff at the elbow
    def block(a, b, w):
        a, b = np.array(a), np.array(b)
        d = b - a; d /= np.linalg.norm(d)
        u = np.cross(d, (0, 0, 1.0)); u /= np.linalg.norm(u)
        v = np.cross(u, d)
        return hull([c + u * x + v * y for c in (a, b) for x in (-w, w) for y in (-w, w)])
    k.add(f"upper{side}", block(SH, EL, 0.062), YELLOW, f"arm{side}", fuse=False, surface="metal")
    WR = np.array((0.28, -0.23, 0.34)) * q
    k.add(f"cuff{side}", outward(ell(EL, (0.07, 0.07, 0.07))), GREY, f"arm{side}", fuse=False, surface="metal")
    k.add(f"fore{side}", block(EL, WR, 0.052), YELLOW, f"arm{side}", fuse=False, surface="metal")
    # the hand: a V of two thick fingers on a broad palm, its palm turned forward and a little out
    ex = unit(s * 1.0, -0.32, 0.0)
    ez = np.array([0, 0.18, 1.0]); ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    n = np.cross(ex, ez) * s                            # forward, the palm's way
    W = WR + n * 0.01
    at = lambda u, w, d=0.0: W + ex * u + ez * w + n * d
    # the palm: a rounded block laid along the hand's own frame
    V, F = ell((0, 0, 0), (0.125, 0.058, 0.1), deform=boxy(2.6))
    k.add(f"hand_palm{side}", (V[:, :1] * ex + V[:, 1:2] * n + V[:, 2:] * ez + at(0.01, 0.09), F), GREY,
          f"arm{side}")
    for j, (a, b) in enumerate((((-0.045, 0.14), (-0.095, 0.325)), ((0.05, 0.14), (0.145, 0.31)))):
        k.add(f"hand_f{j}{side}", blade([at(*a), at((a[0] + b[0]) / 2, (a[1] + b[1]) / 2), at(*b)],
                                         [0.066, 0.063, 0.056], n, 0.62), GREY, f"arm{side}")
    # the palm's side yellow, the back grey: a crisp line through the hand's middle
    def palm(P, W=W, n=n, s=s):
        return inside(-((P - W) @ n), 0.0025) * (P[:, 0] * s > 0.05) * (np.linalg.norm(P - W, axis=1) < 0.45)
    k.paint(palm, YELLOW, on=["hand"])
    return W, n


def add_eyes(k, state):
    look = eye_look(state, lambda u, v: np.tile(EYE, (len(u), 1)), FACE, EYE, line=0.2)
    for s in (1, -1):
        col = look if s == 1 else (lambda a, b, g=look: g(-a, b))
        k.decal(H + (0.03 * s, -HR[1] * 0.93, -0.01), unit(0.3 * s, -1, -0.05), (0.018, 0.021), col, eps=0.0045,
                rings=20, segs=56)


def iron_hands(src, out):
    k = Kit(src, coat="metal")
    add_body(k)
    add_head(k)
    for s in (1, -1):
        add_arm(k, s)
    k.build(voxel=0.007, smooth=4, band=0.005, wband=0.06)
    k.eye_states(lambda s: add_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    iron_hands(*args())
