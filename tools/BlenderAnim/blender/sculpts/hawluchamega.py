# Mega Hawlucha sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hawluchamega.py -- <the export's Hawlucha-Mega.glb> <out.glb>
#
# From the references (10300): Hawlucha as a champion in the ring. The largest masses, in order: two great green
# wings raised in a V, long feathers fanning down and out from red arms held up along their leading edges, red fists
# at the top; a golden ruff of scalloped feathers round the neck and over the shoulders, each scallop tipped black;
# the mask now black trimmed gold, round white eyes with small black pupils, a gold beak open on a pink mouth; orange
# flames of a crest streaming up and back from the crown to the model's right; red shoulders and back, a white belly
# and a white skirt of pointed down at the hips; pale yellow legs and feet.
#
# The arms are raised as the pictures raise them, since they are the wings' leading edges: the wings stand in a V
# of some 45 degrees and fan down from them, so both cameras see their faces.
#
# Grown from hawlucha.py (its body, beak, mouth, legs and down, hands and the feather fan, copied here: the base
# builds in one function that takes no arguments that reach what must change), by treecko.py's leaf_blade and
# helioptile.py's toes as the base uses them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, outward, srgb, inside, tri, unit, polygon, eye_look, args, sibling

T = sibling("treecko")
P = sibling("helioptile")
GREEN, RED, WHITE_, ORANGE = "#1FAA80", "#E0303E", "#F4F2EE", "#F47A22"
GOLD_, BLACK_, LEGS = "#F2C23A", "#1E1C22", "#E0CE7A"
MOUTH = "#E07484"
INK, WHITE = srgb("#15161C"), np.array([1.0, 1.0, 1.0])
VOXEL = 0.005

H = np.array([0.0, -0.032, 0.42])             # the head
HR = np.array([0.08, 0.082, 0.09])
EYE_DIR = unit(0.56, -0.78, 0.24)
EYE_HALF = (0.03, 0.026)


def edge_of(Pn):
    q = (Pn - H) / HR
    a = np.arctan2(np.abs(q[:, 0]), -q[:, 1])
    return q, np.interp(a, [0, 0.45, 0.9, 1.5, 2.2, 3.15], [-0.12, -0.18, -0.3, -0.42, -0.62, -0.7])


def mask(Pn, grow=0.0):
    """The black mask over the crown and the back, down past the eyes to the beak at the front."""
    q, edge = edge_of(Pn)
    near = inside((q ** 2).sum(1) - 1.25, 0.02) * (Pn[:, 2] > 0.33)
    return inside(edge - grow - q[:, 2], 0.012) * near


def chest_edge(Pn):
    ph = np.arctan2(Pn[:, 0], -Pn[:, 1])
    base = np.interp(np.abs(ph), [0, 0.5, 1.2, 1.9, 3.15], [0.255, 0.28, 0.3, 0.27, 0.2])
    return base + 0.012 * tri(ph * 9 / (2 * math.pi))


def hawlucha_mega(src, out):
    k = Kit(src, coat="fur")

    # The body: a white egg of a belly, red shoulders over it, a short thick neck into the head
    k.add("belly", ell((0, 0.01, 0.225), (0.086, 0.076, 0.1)), WHITE_, "body")
    k.add("shoulders", ell((0, 0.006, 0.305), (0.1, 0.072, 0.058)), RED, "body")
    k.add("neck", ell((0, -0.012, 0.345), (0.058, 0.056, 0.05)), WHITE_, "head")
    k.add("head", ell(H, HR), BLACK_, "head")
    k.add("cheek", ell(H + (0, -0.03, -0.035), (0.06, 0.052, 0.046)), WHITE_, "head")
    k.paint(lambda Pn: inside(chest_edge(Pn) - Pn[:, 2], 0.0025) * inside(np.abs(Pn[:, 0]) - 0.12, 0.004)
            * inside(Pn[:, 2] - 0.345, 0.004) * inside(0.17 - Pn[:, 2], 0.004), RED)
    k.paint(lambda Pn: inside(Pn[:, 2] - chest_edge(Pn), 0.0025) * inside(np.abs(Pn[:, 0]) - 0.1, 0.004)
            * inside(0.2 - Pn[:, 2], 0.004) * inside(Pn[:, 1] + 0.0, 0.004), WHITE_, on=["belly", "shoulders", "neck"])
    # the mask: gold along its lower edge, black above that, a gold stripe up the crown's middle
    k.paint(mask, GOLD_)
    k.paint(lambda Pn: mask(Pn, grow=0.16), BLACK_)
    k.paint(lambda Pn: mask(Pn, grow=0.16) * inside(np.abs(Pn[:, 0]) - 0.011, 0.003)
            * inside(Pn[:, 1] - (H[1] + 0.03), 0.004) * inside(H[2] + 0.01 - Pn[:, 2], 0.004), GOLD_)
    k.paint(lambda Pn: (1 - mask(Pn)) * inside(((Pn - H) / (HR * 1.15)) ** 2 @ np.ones(3) - 1, 0.02)
            * (Pn[:, 2] > 0.345), WHITE_)

    # The gold beak, open: an upper hook and a short lower jaw
    b0 = H + np.array([0, -0.072, 0.014])
    k.add("beak", hull([b0 + (0.018, 0.0, 0.01), b0 + (-0.018, 0.0, 0.01), b0 + (0.014, 0.0, -0.016),
                        b0 + (-0.014, 0.0, -0.016), b0 + (0.009, -0.034, 0.0), b0 + (-0.009, -0.034, 0.0),
                        b0 + (0, -0.046, -0.022), b0 + (0, 0.012, 0.03)]), GOLD_, "head", fuse=False, surface="shell")
    j0 = b0 + np.array([0, 0.004, -0.04])
    k.add("jaw", hull([j0 + (0.014, 0.0, 0.006), j0 + (-0.014, 0.0, 0.006), j0 + (0.01, 0.0, -0.008),
                       j0 + (-0.01, 0.0, -0.008), j0 + (0, -0.026, -0.002)]), GOLD_, "head", fuse=False,
          surface="shell")
    # the open mouth between them, pink on the white
    k.decal(P.on_ell(H + (0, -0.03, -0.035), (0.06, 0.052, 0.046), (0, -0.9, -0.3)) + (0, 0, 0.004),
            unit(0, -0.9, -0.3), (0.02, 0.016), MOUTH, eps=0.003, rings=10, segs=40)

    # The flames of the crest: orange tongues from the back of the crown, up, back and out to +X
    knot = H + np.array([0.012, 0.05, 0.06])
    k.add("knot", outward(ell(knot, (0.02, 0.018, 0.018))), ORANGE, "head", fuse=False, surface="cloth")
    for j, (d, ln) in enumerate([((0.35, 0.45, 0.85), 0.14), ((0.6, 0.5, 0.6), 0.13), ((0.15, 0.6, 0.78), 0.11),
                                 ((0.7, 0.35, 0.95), 0.1)]):
        k.add(f"flame{j}", T.leaf_blade(knot, d, (0.25, -0.9, 0.3), ln, 0.024, 0.016, widest=0.35, tip=0.0),
              ORANGE, "head", fuse=False, surface="cloth")

    # The golden ruff round the neck and over the shoulders, scalloped, each scallop tipped black
    k.add("ruff", ell((0, -0.006, 0.322), (0.122, 0.1, 0.05)), GOLD_, "body")
    k.paint(lambda Pn: np.ones(len(Pn)), GOLD_, on=["ruff"])     # over the red of the shoulders
    def scallop(Pn):
        ph = np.arctan2(Pn[:, 0], -Pn[:, 1])
        return 0.296 + 0.012 * tri(ph * 11 / (2 * math.pi))
    k.paint(lambda Pn: inside(scallop(Pn) + 0.009 - Pn[:, 2], 0.002) * inside(Pn[:, 2] - scallop(Pn) - 0.004, 0.002)
            * (np.hypot(Pn[:, 0], Pn[:, 1]) > 0.07), BLACK_, on=["ruff"])
    for j in range(12):
        ph = (j + 0.5) / 12 * 2 * math.pi
        d = np.array([math.sin(ph), -math.cos(ph) * 0.84, 0.0])
        base = np.array([0, -0.006, 0.305]) + d * 0.112
        tip = base + d * 0.04 + np.array([0, 0, -0.04])
        k.add(f"ruffpt{j}", hull([base + (0, 0, 0.012), base + (0, 0, -0.012), base + np.cross(d, (0, 0, 1)) * 0.016,
                                  base - np.cross(d, (0, 0, 1)) * 0.016, base - d * 0.01, tip]), GOLD_, "body",
              fuse=False)

    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # Legs: white thighs with a skirt of pointed down at the hips, pale yellow shins and feet
        k.add(f"thigh{side}", ell(np.array((0.054, 0.004, 0.13)) * m, (0.054, 0.06, 0.07)), WHITE_, f"leg{side}")
        for j in range(7):
            a = -1.1 + 2.2 * j / 6
            dd = np.array([math.sin(a) * 0.9 + 0.35, -math.cos(a) * 0.9, -0.75])
            base = np.array((0.058 + 0.045 * math.sin(a), 0.004 - 0.05 * math.cos(a), 0.12))
            tip = base + unit(*dd) * 0.045
            k.add(f"down{side}{j}", hull([(base + (0.014, 0, 0)) * m, (base - (0.014, 0, 0)) * m,
                                          (base + (0, 0.014, 0)) * m, (base - (0, 0.014, 0)) * m,
                                          (base + (0, 0, 0.014)) * m, tip * m]), WHITE_, f"leg{side}", fuse=False)
        k.add(f"shin{side}", tube(np.array([(0.058, -0.004, 0.085), (0.06, -0.014, 0.025)]) * m, [0.022, 0.02]),
              LEGS, f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.061, -0.03, 0.012)) * m, (0.025, 0.037, 0.012)), LEGS, f"leg{side}",
              fuse=False, surface="shell")
        P.toes(k, f"toe{side}", f"leg{side}", np.array((0.062, -0.06, 0.009)) * m, (0.2 * s, -1, 0), 0.03, 0.0085,
               colour=LEGS)

        # Arms raised along the wings' leading edges, red fists at the top, three fingers tipped gold curled over
        sh, el, wr = np.array((0.062, 0.0, 0.31)), np.array((0.14, 0.0, 0.365)), np.array((0.17, -0.03, 0.44))
        k.add(f"pad{side}", ell(np.array((0.08, 0.0, 0.315)) * m, (0.048, 0.048, 0.042)), RED, f"wing{side}")
        k.add(f"arm{side}", tube(np.array([sh, el, wr]) * m, [0.031, 0.027, 0.024]), RED, f"wing{side}")
        hand = wr + np.array([0, -0.006, 0.022])
        k.add(f"hand{side}", ell(hand * m, (0.028, 0.026, 0.026)), RED, f"wing{side}")
        f = unit(0.1, -0.35, 1.0)
        sd = unit(*np.cross(f, (0, 1, 0)))
        for j, o in enumerate((-1, 0, 1)):
            b = hand + f * 0.018 + sd * o * 0.014
            tip = b + unit(*(f * 0.5 + np.array([0, -1.0, -0.2]))) * 0.02
            k.add(f"finger{side}{j}", outward(tube([b * m, tip * m], [0.009, 0.009])), RED, f"wing{side}", fuse=False)
            k.add(f"nail{side}{j}", outward(ell((tip + unit(*(tip - b)) * 0.006) * m, (0.009, 0.009, 0.009), sub=2)),
                  GOLD_, f"wing{side}", fuse=False, surface="shell")

        # The wing: a fan of long green feathers hanging down and out from the raised arm, the outer ones reaching
        # up past the fist; their faces turned toward the front so both cameras see them
        roots, dirs, lens = [], [], []
        for j in range(8):
            t = j / 7
            root = sh + (el - sh) * min(t * 1.8, 1) + (wr - el) * max(t * 1.8 - 1, 0) + np.array([0.0, 0.012, -0.018])
            d = unit(*((1 - t) * np.array([0.32, 0.25, -1.0]) + t * np.array([1.0, 0.25, 0.2])))
            ln = 0.15 + 0.08 * math.sin(math.pi * (0.3 + 0.55 * t))
            roots.append(root); dirs.append(d); lens.append(ln)
        for j, (d, ln) in enumerate((((0.75, 0.2, 0.75), 0.19), ((0.45, 0.2, 1.0), 0.15))):
            roots.append(wr + np.array([0.0, 0.012, 0.0])); dirs.append(unit(*d)); lens.append(ln)
        fc = (0.55 * s, -0.83, 0.1)
        for j, (root, d, ln) in enumerate(zip(roots, dirs, lens)):
            k.add(f"feather{side}{j}", T.leaf_blade(root * m, d * m, fc, ln, 0.052, 0.02, widest=0.4, tip=0.004),
                  GREEN, f"wing{side}", fuse=False)
        # a web behind the fan's inner half, so the far wing is no comb from behind
        web = []
        fcu = unit(0.55, -0.83, 0.1)
        for root, d, ln in zip(roots, dirs, lens):
            for p in (root, root + d * ln * 0.55):
                web += [(p + fcu * 0.006) * m, (p - fcu * 0.006) * m]
        k.add(f"web{side}", hull(web), GREEN, f"wing{side}", fuse=False)

    k.build(voxel=VOXEL, smooth=4, band=0.005, wband=0.04)

    # The eyes: round and white, a small black pupil, a black rim, cut by the mask's brow sloping to the beak
    def top(u):
        return 0.4 + 0.3 * (u + 1) / 2

    def open_eye(u, v):
        rho = np.sqrt(u * u + v * v)
        c = np.tile(WHITE, (len(u), 1))
        c[((u + 0.08) / 0.2) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = INK
        c[rho > 0.84] = INK
        c[np.abs(v - top(u)) < 0.14] = INK
        return c
    outline = [(math.cos(a), min(math.sin(a), top(math.cos(a)) + 0.12)) for a in np.linspace(0, 2 * math.pi, 48,
                                                                                            endpoint=False)]
    E = P.on_ell(H, HR, EYE_DIR)

    def lay(state):
        look = eye_look(state, open_eye, srgb(BLACK_), srgb("#B8B8C4"), line=0.16)
        for s in (1, -1):
            shape = polygon([(u * s, v) for u, v in outline]) if state in ("Open", "Fierce") else None
            col = look if s > 0 else (lambda u, v, f_=look: f_(-u, v))
            k.decal(E * np.array([s, 1, 1]), EYE_DIR * np.array([s, 1, 1]), EYE_HALF, col, up=(0, 0, 1), eps=0.003,
                    outline=shape, rings=20, segs=56)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    hawlucha_mega(*args())
