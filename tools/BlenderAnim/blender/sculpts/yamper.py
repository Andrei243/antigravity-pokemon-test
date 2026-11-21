# Yamper sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yamper.py -- <the export's Yamper.glb> <out.glb>
#
# From the references: a small tan corgi, a long round loaf of a body low on four short white legs with round paws. Its
# second mass is a great round yellow ruff hanging under the chin and wrapping the neck, as wide as the body. The head is
# round and sits forward on the ruff: tan over the top and sides, a white blaze up the middle of the forehead widening
# over the whole lower face and muzzle; big round eyes in yellow rings, pale green irises with a dark pupil and a glint;
# a small yellow nose; the mouth open, dark inside, a pink tongue lolling out. Big pointed ears: the right one up, the
# left one flopped out to the side showing its yellow lining edged in green. A yellow lightning bolt of a tail rising
# from the rump, a heart-shaped mark on the rump, green round yellow.
#
# Written to be grown from (Boltund: sibling("yamper")): the eye, the ear and its lining, the paws, the nose, the
# tongue and the bolt are one function each.
# Reference ratios (3D picture): the head 0.31 of the height (ears out) and 0.55 of the body's width... the ruff as wide
# as the body, the body 0.42 of the height deep, legs showing 0.1 of the height, the upright ear 0.25 of it.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, slab, srgb, inside, unit, eye_look, polygon, args

TAN, WHITE, YELLOW, GREEN = "#C9A57A", "#F7F4EC", "#F6E43C", "#A8D27A"
IRIS, IRISRIM, PUPIL, INK = "#D8F0A0", "#7FA850", "#2A3A20", "#3A2A1E"
MOUTH, TONGUE, NOSE = "#5A2426", "#E0707C", "#F2D63A"

BC, BR = np.array([0.0, 0.025, 0.122]), np.array([0.09, 0.14, 0.086])
HC, HR = np.array([0.0, -0.105, 0.27]), np.array([0.097, 0.087, 0.08])
VOXEL = 0.0045


def on_ell(c, r, x, z, front=True):
    c, r = np.array(c, float), np.array(r, float)
    s = 1 - ((x - c[0]) / r[0]) ** 2 - ((z - c[2]) / r[2]) ** 2
    y = c[1] + (-1 if front else 1) * r[1] * math.sqrt(max(s, 0.0))
    return np.array((x, y, z))


def normal_at(c, r, p):
    n = (np.array(p, float) - np.array(c, float)) / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def leg(k, name, top, foot, r, pr, col, bone):
    """A short leg and a round paw (mirrored)."""
    k.add(name, tube([top, ((top[0] + foot[0]) / 2, (top[1] + foot[1]) / 2, (top[2] + foot[2]) / 2 + 0.005),
                      (foot[0], foot[1] + 0.006, foot[2] + 0.012)], [r * 1.12, r, r * 0.95]), col, bone, mirror=True)
    k.add(name + "paw", ell(foot, pr), col, bone, mirror=True)


def ear(k, name, base, tip, r, col, flat, f=0.45):
    base, tip = np.array(base, float), np.array(tip, float)
    k.add(name, blade([base, base + (tip - base) * 0.5, tip], [r, r * 0.7, 0.005], flat, f), col, "head")


def ear_lining(k, base, tip, width, front, core, rim, on):
    """The inside of an ear on its face toward front: a core of one colour edged by a rim of another, crisp."""
    base, tip, fr = np.array(base, float), np.array(tip, float), unit(*front)
    d = tip - base
    L = np.linalg.norm(d); d = d / L
    def part(P, w):
        q = P - base
        t = q @ d
        across = q - np.outer(t, d)
        depth = across @ fr
        side = across - np.outer(depth, fr)
        wid = w * (1 - np.clip(t / (L * 0.95), 0, 1)) ** 0.8
        return inside(np.linalg.norm(side, axis=1) - wid, 0.002) * (t > L * 0.08) * (depth > 0.0)
    k.paint(lambda P: part(P, width), rim, on=on)
    k.paint(lambda P: part(P, width * 0.62), core, on=on)


def ring_eye(iris=IRIS, rim=IRISRIM, pupil=PUPIL, yellow=YELLOW):
    """A round eye: a pale green iris edged darker, a dark pupil, a white glint; its lid the yellow ring round it."""
    I, R, P, W, Y = srgb(iris), srgb(rim), srgb(pupil), srgb("#FFFFFF"), srgb(yellow)
    def look(u, v):
        c = np.tile(R, (len(u), 1))
        rr = np.sqrt(u * u + v * v)
        c[rr < 0.8] = I
        c[((u + 0.05) / 0.3) ** 2 + ((v + 0.02) / 0.4) ** 2 < 1] = P
        c[((u - 0.2) / 0.17) ** 2 + ((v - 0.28) / 0.17) ** 2 < 1] = W
        return c
    return look


def lay_eyes(k, state, E, n, half, look, lid, ink, line=0.15):
    f = eye_look(state, look, srgb(lid), srgb(ink), line=line)
    for side in (1, -1):
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(E) * np.array([side, 1, 1.0]), np.array(n) * np.array([side, 1, 1.0]), half, col, eps=0.0025,
                rings=20, segs=56)


def bolt(origin, ex, scale, thick):
    """A lightning bolt of a tail: a zigzag plate rising from origin, in the plane of ex and straight up."""
    pts = [(-0.012, 0.0), (0.016, 0.0), (0.03, 0.05), (0.052, 0.046), (0.072, 0.118), (0.03, 0.07), (0.012, 0.074)]
    return slab([(a * scale, b * scale) for a, b in pts], origin, ex, (0, 0, 1), thick)


def heart(n=48):
    """A heart's outline in u, v (point down), star-shaped about its middle."""
    pts = []
    for t in np.linspace(0, 2 * math.pi, n, endpoint=False):
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((x / 17, (y + 2) / 15))
    return pts


def tongue(c, d, length, w):
    """A tongue lolling out of the mouth: a flattened rounded strip from c along d (forward and down)."""
    c, d = np.array(c, float), unit(*d)
    return blade([c, c + d * length * 0.55, c + d * length], [w, w * 0.95, w * 0.7], (0, 0, 1), 0.38)


def yamper(src, out):
    k = Kit(src, coat="fur")

    # --- the body: a long round loaf, low ----------------------------------------------------------------------
    k.add("body", ell(BC, BR), TAN, "body")
    k.add("rump", ell((0, 0.1, 0.118), (0.088, 0.075, 0.078)), TAN, "body")

    # --- four short white legs with round paws ----------------------------------------------------------------
    PR = (0.03, 0.036, 0.02)
    leg(k, "leg", (0.052, -0.06, 0.08), (0.056, -0.068, 0.02), 0.03, PR, WHITE, "leg{s}")
    leg(k, "hind", (0.054, 0.085, 0.08), (0.058, 0.08, 0.02), 0.032, PR, WHITE, "hind{s}")
    k.paint(lambda P: inside(0.05 - P[:, 2], 0.004) * (np.abs(P[:, 0]) > 0.02), TAN, on=["body", "rump", "leg", "hind"])

    # --- the great yellow ruff under the chin, wrapping the neck ------------------------------------------------
    k.add("ruff", ell((0, -0.085, 0.148), (0.1, 0.085, 0.09)), YELLOW, "body")
    k.add("collar", ell((0, -0.06, 0.2), (0.093, 0.07, 0.055)), YELLOW, "head")

    # --- the head ----------------------------------------------------------------------------------------------
    k.add("head", ell(HC, HR), TAN, "head")
    MC, MR = np.array((0, -0.172, 0.245)), np.array((0.052, 0.042, 0.04))
    k.add("muzzle", ell(MC, MR), WHITE, "head")
    # the white blaze: up the forehead's middle, widening over the whole lower face
    def blaze(P):
        p = P - HC
        th = np.abs(np.arctan2(p[:, 0], -p[:, 1]))
        z = p[:, 2] / HR[2]
        wide = np.where(z > 0.25, 0.3 + 0.12 * (1 - z), 0.3 + 1.2 * np.clip(0.25 - z, 0, 1) / 0.6)
        return inside(th - wide, 0.03) * (p[:, 1] < 0.02) * (np.linalg.norm(p / (HR * 1.35), axis=1) < 1)
    k.paint(blaze, WHITE, on=["head", "muzzle"])

    # ears: the right one up, the left flopped out to the side, each lined yellow edged green on its front
    ear(k, "earR", (0.052, -0.09, 0.322), (0.078, -0.07, 0.445), 0.04, TAN, (0.3, 1, 0))
    ear(k, "earL", (-0.06, -0.085, 0.315), (-0.175, -0.07, 0.385), 0.044, TAN, (0.2, 1, 0.3))
    ear_lining(k, (0.052, -0.09, 0.322), (0.078, -0.07, 0.445), 0.022, (-1, -0.35, 0), YELLOW, GREEN, ["earR"])
    ear_lining(k, (-0.06, -0.085, 0.315), (-0.175, -0.07, 0.385), 0.03, (-0.05, -1, 0.35), YELLOW, GREEN, ["earL"])

    # the yellow rings round the eyes (in every state)
    EX, EZ = 0.05, 0.286
    E = on_ell(HC, HR, EX, EZ)
    en = unit(*(normal_at(HC, HR, E) + np.array((0, -0.45, 0))))
    k.decal(E, en, (0.031, 0.033), YELLOW, eps=0.002, rings=12, segs=48, mirror=True)

    # a small yellow nose; the mouth open under it, dark, a pink tongue lolling out
    k.add("nose", ell(MC + (0, -0.04, 0.026), (0.014, 0.01, 0.009)), NOSE, "head", fuse=False, surface="skin")
    mp = on_ell(MC, MR, 0.0, MC[2] - 0.016)
    mo = [(math.cos(t), math.sin(t) * (0.55 if math.sin(t) > 0 else 1.0)) for t in
          np.linspace(0, 2 * math.pi, 48, endpoint=False)]
    k.decal(mp + (0, 0, -0.004), unit(0, -1, -0.25), (0.05, 0.03), MOUTH, eps=0.0025, rings=12, segs=64,
            outline=polygon(mo))
    k.add("tongue", tongue(mp + (0.008, 0.006, -0.018), (0.12, -0.5, -1), 0.05, 0.016), TONGUE, "head", fuse=False,
          surface="skin")

    # --- the tail: a yellow bolt rising from the rump, its face turned out to +X --------------------------------
    k.add("tailroot", ell((0, 0.15, 0.185), (0.024, 0.024, 0.022)), YELLOW, "tail")
    k.add("tail", bolt((0.0, 0.155, 0.18), unit(0.57, 0.82, 0.0), 1.15, 0.018), YELLOW, "tail", fuse=False)

    # --- the heart on the rump, green round yellow --------------------------------------------------------------
    hp = np.array((0.0, 0.17, 0.13))
    k.decal(hp, (0, 1, 0.15), (0.042, 0.042), GREEN, eps=0.002, rings=14, segs=96, outline=polygon(heart()))
    k.decal(hp + (0, 0, 0.002), (0, 1, 0.15), (0.026, 0.026), YELLOW, eps=0.0035, rings=12, segs=96,
            outline=polygon(heart()))

    k.build(voxel=VOXEL, smooth=3, band=0.004)

    # big round eyes in the yellow rings
    look = ring_eye()
    k.eye_states(lambda s: lay_eyes(k, s, E, en, (0.021, 0.023), look, YELLOW, INK))
    k.export(out)


if __name__ == "__main__":
    yamper(*args())
