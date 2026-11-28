# Nufarina sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/nufarina.py -- <the export's Nufarina.glb> <out.glb>
#
# Carpathia's water lily spirit (No. 10157, Grass/Fairy), Nufarel grown (its petals, pad, bud, palette and way of
# laying a face imported from nufarel.py), from its picture (docs/art/reference/gemini/carpathia/10157-Nufarina.jpg)
# and its brief (CP-dex-3b-river-and-caves-more.md, 157): a graceful spirit standing in a water lily in full bloom, its
# gown: three rings of petals, white and pale pink without and pink within, spread over a circle of round notched
# green pads. A slender green stem of a body rises from the bloom's heart, a skirt of leaves at its waist and a collar
# of leaves at its shoulders, thin green arms ending in leaf hands, one raised to its chest under the chin, the other
# held down and forward, a curling tendril at its hip. A pale face with eyes shut, a faint smile and pink cheeks,
# framed by green sepals and crowned by Nufarel's pink bud. A small blue dragonfly rests on a petal of the bloom (+X).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, outward, srgb, inside, smoothstep, unit, eye_look, args, sibling

n_ = sibling("nufarel")
petal, around, lily_pad, board = n_.petal, n_.around, n_.lily_pad, n_.board
PETAL, PETAL2, PETALTIP, SEPAL, SEPALDK, PAD, PADDK = (n_.PETAL, n_.PETAL2, n_.PETALTIP, n_.SEPAL, n_.SEPALDK,
                                                        n_.PAD, n_.PADDK)
WHITE, BLUSHW = "#F9F1F3", "#F3DCE3"
STEM, LEAF, LEAFDK = "#6FA24E", "#7DAE56", "#5E8E42"
SKIN, BLUSH, MOUTH, INK = "#F6EEE4", "#EFA6B2", "#B85C6E", "#3A2A2E"
FLY, FLYWING = "#4E86C8", "#BFE0F4"
INKC = srgb(INK)


class Bud(n_.Cfg):
    """Nufarel's bud grown smaller, crowning her head."""
    g = 0.67
    C = np.array([0.0, 0.012, 0.912])
    R = np.array([0.076, 0.076, 0.066])


FACE_C = np.array([0.0, -0.004, 0.8])
FACE_R = np.array([0.081, 0.074, 0.084])


# --- the gown: the bloom and its pads -----------------------------------------------------------------------------
def gown(k):
    for j, a in enumerate(range(30, 360, 60)):
        c = around(a, 0.27)
        lily_pad(k, (c[0], c[1], 0.0), 0.15, a, "gown", 0.02, name=f"pad{j}")
    lily_pad(k, (0.0, 0.0, 0.002), 0.2, 200, "gown", 0.02, name="padmid")
    rings = [  # (count, phase, base radius, base height, elevation, length, width, colours)
        (12, 0, 0.05, 0.035, 12, 0.36, 0.075, (WHITE, BLUSHW)),
        (10, 18, 0.045, 0.06, 42, 0.31, 0.072, (BLUSHW, WHITE)),
        (8, 0, 0.035, 0.08, 66, 0.3, 0.066, (PETAL, PETAL2)),
    ]
    for ri, (n, phase, r0, z0, el, ln, w, cols) in enumerate(rings):
        e = math.radians(el)
        for j in range(n):
            a = phase + 360 * j / n
            rh = around(a)
            d = math.cos(e) * rh + math.sin(e) * np.array([0, 0, 1.0])
            face = -math.sin(e) * rh + math.cos(e) * np.array([0, 0, 1.0])
            base = rh * r0 + np.array([0, 0, z0])
            k.add(f"bloom{ri}_{j}", petal(base + d * ln / 2, face, d, ln, w, 0.024, curve=-0.7, bend=-0.08),
                  cols[j % 2], "gown", fuse=False, surface="leaf")
    k.add("heart", ell((0, 0, 0.07), (0.055, 0.055, 0.035)), LEAF, "gown")


def dragonfly(k):
    """A small blue dragonfly resting near the tip of a front petal on +X, its wings raised in a shallow V."""
    a = math.radians(60)
    rh = around(60)
    p = rh * 0.35 + np.array([0, 0, 0.142])
    d = unit(*(np.cross((0, 0, 1.0), rh)))
    k.add("flybody", outward(tube([p - d * 0.045, p, p + d * 0.03], [0.011, 0.012, 0.0105])), FLY, "gown", fuse=False,
          surface="shell")
    k.add("flyhead", ell(p + d * 0.042, (0.015, 0.015, 0.014), sub=3), FLY, "gown", fuse=False, surface="shell")
    side = rh
    for j, (along, sgn) in enumerate(((0.012, 1), (0.012, -1), (-0.004, 1), (-0.004, -1))):
        out = unit(*(side * sgn + np.array([0, 0, 0.45])))
        sweep = unit(*(out + d * (0.25 if along > 0 else -0.25)))
        root = p + d * along + np.array([0, 0, 0.012])
        W = [(0.0, -0.008), (0.05, -0.012), (0.062, 0.0), (0.05, 0.01), (0.0, 0.007)]
        k.add(f"flywing{j}", slab(W, root, sweep, d, 0.004), FLYWING, "gown", fuse=False, surface="shell")


# --- the body ------------------------------------------------------------------------------------------------------
def body(k):
    k.add("stem", tube([(0, 0, 0.06), (0.012, -0.01, 0.25), (0, -0.005, 0.44), (0, 0, 0.56)],
                       [0.032, 0.028, 0.03, 0.036]), STEM, "body")
    k.add("chest", ell((0, 0, 0.6), (0.058, 0.046, 0.075)), STEM, "body")
    k.add("neck", tube([(0, 0, 0.64), (0, -0.003, 0.74)], [0.026, 0.024]), STEM, "head")
    k.add("face", ell(FACE_C, FACE_R), SKIN, "head", surface="skin")
    # the skirt of leaves at the waist, hanging down and out
    for j in range(7):
        a = 360 * j / 7 + 10
        rh = around(a)
        d = unit(*(rh * 0.55 + np.array([0, 0, -1.0])))
        base = rh * 0.04 + np.array([0, 0, 0.56])
        k.add(f"skirt{j}", petal(base + d * 0.075, rh + np.array([0, 0, 0.4]), d, 0.17, 0.048, 0.02, curve=0.6,
                                 bend=-0.1), LEAF if j % 2 else LEAFDK, "body", fuse=False, surface="leaf")
    # the collar of leaves at the shoulders
    for s in (1, -1):
        for j, (dx, dy, dz, ln) in enumerate(((1.0, 0.1, -0.15, 0.1), (0.7, 0.6, -0.3, 0.09), (0.7, -0.5, -0.3, 0.08))):
            d = unit(dx * s, dy, dz)
            base = np.array([0.03 * s, dy * 0.02, 0.665])
            k.add(f"collar{'R' if s > 0 else 'L'}{j}", petal(base + d * ln / 2, (0, 0, 1.0), d, ln, 0.03, 0.016,
                                                             curve=0.5), LEAF if j % 2 else LEAFDK, "body", fuse=False,
                  surface="leaf")


def arms(k):
    """The right (+X) hand raised to the chest under the chin; the left held down and forward."""
    R = [(0.05, 0.0, 0.645), (0.115, -0.03, 0.585), (0.1, -0.075, 0.635), (0.062, -0.098, 0.672)]
    k.add("armR", tube(R, [0.018, 0.016, 0.015, 0.014]), STEM, "armR")
    k.add("handR", petal((0.052, -0.108, 0.7), (0.2, -1.0, 0.0), (-0.3, -0.1, 1.0), 0.075, 0.022, 0.014, curve=0.4),
          LEAF, "armR", fuse=False, surface="leaf")
    L = [(-0.05, 0.0, 0.645), (-0.13, -0.025, 0.57), (-0.18, -0.065, 0.51)]
    k.add("armL", tube(L, [0.018, 0.016, 0.014]), STEM, "armL")
    k.add("handL", petal((-0.205, -0.085, 0.485), (0.0, -1.0, 0.3), (-0.6, -0.45, -0.55), 0.07, 0.022, 0.014,
                         curve=0.4), LEAF, "armL", fuse=False, surface="leaf")
    # the tendril from the left hip, curling up at its end
    th = np.linspace(0, 1.6 * math.pi, 9)
    cc = np.array([-0.205, -0.01, 0.5])
    curl = [cc + np.array([-0.028 * math.cos(t) * (1 - 0.25 * t / 5), 0, 0.028 * math.sin(t) * (1 - 0.25 * t / 5)])
            for t in th]
    pts = [np.array([-0.04, 0.02, 0.55]), np.array([-0.11, 0.015, 0.52]), np.array([-0.17, 0.0, 0.49])] + curl[2:]
    k.add("tendril", outward(tube(pts, np.linspace(0.016, 0.012, len(pts)))), STEM, "body", fuse=False,
          surface="leaf")


def head(k):
    """Nufarel's bud on the crown, and green sepals framing the face like a hood."""
    n_.bud(k, Bud, "head", sepals=False)
    for j, a in enumerate((62, 112, 160, 200, 248, 298)):
        o = around(a)
        base = FACE_C + o * FACE_R[0] * 0.92 + np.array([0, 0, -0.01])
        up = unit(*(o * 0.25 + np.array([0, 0, 1.0])))
        k.add(f"hood{j}", petal(base + up * 0.075, o, up, 0.17, 0.05, 0.016, curve=0.7, bend=0.04),
              SEPAL if j % 2 else SEPALDK, "head", fuse=False, surface="leaf")
    # cheeks, in every state
    for s in (1, -1):
        d = unit(0.55 * s, -1.0, -0.25)
        c = n_.np.array(FACE_C) + d / np.linalg.norm(d / FACE_R)
        k.decal(c, d, (0.014, 0.009), BLUSH, eps=0.0022, rings=8, segs=32)
    # a faint smile
    d = unit(0, -1.0, -0.42)
    c = FACE_C + d / np.linalg.norm(d / FACE_R)
    ink = srgb(MOUTH)
    skin = srgb(SKIN)
    def smile(u, v):
        col = np.tile(skin, (len(u), 1))
        col[(np.abs(v - (0.3 - 0.75 * (1 - u * u))) < 0.3) & (np.abs(u) < 0.85)] = ink
        return col
    k.decal(c, d, (0.017, 0.008), smile, eps=0.0022, rings=10, segs=40)


def eyes(k):
    L = srgb(SKIN)
    line = 0.2
    def shut(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (0.05 - 0.4 * (1 - u * u))) < line)] = INKC
        c[(u > 0.55) & (u < 0.95) & (np.abs(v - (0.05 + 0.6 * (u - 0.55))) < line * 0.8)] = INKC    # a lash outward
        return c
    def fierce(u, v):
        c = np.tile(L, (len(u), 1))
        c[(np.abs(u) < 0.85) & (np.abs(v - (-0.1 + 0.35 * u)) < line)] = INKC
        return c
    def lay(state):
        look = {"Open": shut, "Shut": shut, "Fierce": fierce}.get(state) or eye_look(state, shut, L, INKC, line=line)
        d = unit(0.34, -1.0, 0.06)
        c = FACE_C + d / np.linalg.norm(d / FACE_R)
        k.decal(c, d, (0.019, 0.013), look, eps=0.0022, rings=20, segs=56, mirror=True)
    return lay


def nufarina(src, out_path):
    k = Kit(src, coat="leaf")
    gown(k)
    dragonfly(k)
    body(k)
    arms(k)
    head(k)
    k.build(voxel=0.0055, band=0.005, wband=0.04)
    board(k)
    k.eye_states(eyes(k))
    k.export(out_path)


if __name__ == "__main__":
    nufarina(*args())
