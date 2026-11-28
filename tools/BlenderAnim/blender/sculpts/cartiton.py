# Cartiton sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cartiton.py -- <the export's Cartiton.glb> <out.glb>
# Carpathia No. 10046 (plan 46), the plough mole, Ground/Steel. Grown from cartitel.py (sibling("cartitel")): its
# colours, claws, feet, mound of earth and way of making the face kept. From its picture: a big thickset mole of
# dark velvet, hunched forward, the head carried low in front of high shoulders; a grey belly; the mound of earth
# grown into a cap over head and shoulders with grey shards of rock in it; a long pink snout reaching forward, its tip
# glowing amber like a miner's lamp; small eyes under the mound's brim, narrowed and scowling; thick arms down to pink
# wrists, each gripping an enormous ploughshare of dark iron that hangs below the hand, worn pale along its edge,
# with dark claws curling over it; short thick legs on pink feet; a pink tail behind.
# The skeleton: body (0, 0, 0.42), head (0, -0.1, 0.6), arms (+-0.19, -0.06, 0.52), legs (+-0.11, 0, 0.2),
# tail (0, 0.16, 0.22).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, smoothstep, unit, eye_look, args, axes, sibling

ct = sibling("cartitel")

BLACK, BELLY = ct.BLACK, "#9C9AA2"
PINK, CLAW = ct.PINK, ct.CLAW
EARTH, EARTH_L = ct.EARTH, ct.EARTH_L
IRON, IRON_EDGE, IRON_CLAW = "#4C4A50", "#8E8C90", "#3A383E"
ROCK, ROCK_L = "#77757C", "#93919A"
LAMP, LAMP_GLOW = "#FF9A3A", "#FF8A2A"
EYE_GREY, PUPIL = "#B4B2BC", "#1C1416"

HEAD_C, HEAD_R = np.array([0.0, -0.15, 0.585]), np.array([0.138, 0.125, 0.112])


def ploughshare(k, side, root, bone):
    """An iron ploughshare hanging below the hand: a thick plate turned half to the front, half outward, with a pale
    worn edge along its cutting side and dark claws curling over its top."""
    s = side
    down = unit(0.12 * s, -0.42, -1.0)
    ez = -down
    nn = unit(0.55 * s, -0.83, 0.0)
    nn = nn - ez * (nn @ ez); nn /= np.linalg.norm(nn)
    ex = np.cross(ez, nn)
    outline = [(0.0, 0.02), (0.075, 0.0), (0.095, -0.09), (0.05, -0.18), (0.0, -0.225), (-0.055, -0.15),
               (-0.075, -0.05), (-0.05, 0.01)]
    k.add(f"share{'R' if s > 0 else 'L'}", slab(outline, root, ex, ez, 0.034), IRON, bone, fuse=False, surface="metal")
    # the worn edge: a thinner, lighter plate a little larger along the cutting side
    edge = [(0.082, -0.005), (0.103, -0.09), (0.056, -0.188), (0.0, -0.238), (-0.062, -0.155), (-0.05, -0.15),
            (0.0, -0.205), (0.045, -0.172), (0.085, -0.09), (0.068, -0.01)]
    k.add(f"edge{'R' if s > 0 else 'L'}", slab(edge, root, ex, ez, 0.024), IRON_EDGE, bone, fuse=False, surface="metal")
    # scars: short pale gouges standing just off its outer face
    for i, (a, b, ln) in enumerate(((0.02, -0.08, 0.05), (-0.02, -0.12, 0.04))):
        c = np.array(root) + ex * a + ez * b + nn * 0.019
        d = ez * 0.5 + ex * 0.8; d /= np.linalg.norm(d)
        pts = [c + d * ln / 2 + nn * 0.004, c - d * ln / 2 + nn * 0.004, c + d * ln / 2 - nn * 0.006,
               c - d * ln / 2 - nn * 0.006, c + np.cross(nn, d) * 0.006, c - np.cross(nn, d) * 0.006]
        k.add(f"scar{i}{'R' if s > 0 else 'L'}", hull(pts), IRON_EDGE, bone, fuse=False, surface="metal")
    # dark claws curling over the share's top
    for i, dx in enumerate((-0.035, 0.0, 0.035)):
        c = np.array(root) + ex * dx + nn * 0.03 + ez * 0.04
        ct.claw(k, f"iclaw{i}{'R' if s > 0 else 'L'}", c, down * 0.9 + nn * 0.45, 0.09, 0.02, 0.013, IRON_CLAW, bone,
                mirror=False, surface="metal")


def build(src, out):
    k = Kit(src, coat="fur")
    # the thickset trunk, hunched: a deep lower body, the chest and shoulders forward and high
    k.add("body", ell((0, 0.02, 0.31), (0.235, 0.205, 0.25), deform=ct.pear), BLACK, "body")
    k.add("chest", ell((0, -0.06, 0.5), (0.24, 0.19, 0.16)), BLACK, "body")
    k.add("hump", ell((0, 0.02, 0.58), (0.2, 0.17, 0.11)), BLACK, "body")
    k.add("head", ell(HEAD_C, HEAD_R), BLACK, "head")
    k.add("cheek", ell((0, -0.22, 0.56), (0.095, 0.075, 0.075)), BLACK, "head")
    # the long pink snout reaching forward, the lamp on its tip
    k.add("snout", tube([(0, -0.23, 0.57), (0, -0.31, 0.56), (0, -0.375, 0.548)], [0.046, 0.037, 0.032]), PINK, "head",
          surface="skin")
    k.add("tip", ell((0, -0.39, 0.546), (0.036, 0.022, 0.033), rot=(0, 0, 0)), PINK, "head", surface="skin")
    k.add("lamp", ell((0, -0.405, 0.546), (0.024, 0.011, 0.023)), LAMP, "head", fuse=False, glow=LAMP_GLOW, surface="skin")
    k.add("halo", ell((0, -0.398, 0.546), (0.032, 0.008, 0.031)), "#FFB870", "head", fuse=False, glow="#FFA858",
          surface="skin")
    def root(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = (x / 0.06) ** 2 + ((z - 0.565) / 0.05) ** 2
        return inside(np.sqrt(e) - 1, 0.012) * inside(y + 0.23, 0.006)
    k.paint(root, PINK, on=["head", "cheek"])
    # the grey belly
    k.paint(lambda P: inside(np.sqrt((P[:, 0] / 0.15) ** 2 + ((P[:, 2] - 0.3) / 0.2) ** 2) - 1, 0.008)
            * inside(P[:, 1] + 0.1, 0.01), BELLY, on=["body", "chest"])

    # thick arms down to pink wrists, the ploughshares below the hands
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.19, -0.06, 0.52), (0.245, -0.12, 0.42), (0.26, -0.165, 0.32)]) * m,
                                 [0.09, 0.08, 0.072]), BLACK, f"arm{side}")
        k.add(f"wrist{side}", ell(np.array((0.262, -0.19, 0.28)) * m, (0.078, 0.07, 0.07)), PINK, f"arm{side}")
        ploughshare(k, s, np.array((0.256, -0.225, 0.25)) * m, f"arm{side}")
    k.paint(lambda P: inside(P[:, 2] - 0.335, 0.006), PINK, on=["armR", "armL"])

    # short thick legs, pink feet with toes
    k.add("leg", tube([(0.11, 0.0, 0.22), (0.125, -0.02, 0.1)], [0.092, 0.078]), BLACK, "leg{s}", mirror=True)
    ct.foot(k, (0.13, -0.075, 0.034), (0.066, 0.088, 0.034), PINK, 4, 0.017)
    # the pink tail
    k.add("tail", tube([(0, 0.18, 0.22), (0, 0.27, 0.26), (0, 0.315, 0.32)], [0.042, 0.03, 0.016]), PINK, "tail",
          surface="skin")

    # the mound over head and shoulders, shards of rock in it
    ct.mound(k, (0, 0.0, 0.665), (0.205, 0.19, 0.075), EARTH, EARTH_L, n=16, seed=8,
             crumbs=[((0.19, -0.18, 0.6), 0.012), ((-0.2, -0.15, 0.6), 0.011), ((0.24, -0.02, 0.58), 0.013)])
    rs = np.random.RandomState(4)
    for i, (x, y, lean) in enumerate(((-0.12, -0.06, -0.5), (0.1, -0.1, 0.45), (0.0, 0.06, 0.1), (0.15, 0.06, 0.6),
                                       (-0.13, 0.08, -0.6), (0.04, -0.15, 0.2))):
        base = np.array([x, y, 0.69 - 0.6 * (x * x + y * y)])
        d = unit(0.55 * lean, 0.25 * rs.randn(), 1.0)
        a, b = axes(d)
        h = 0.085 + 0.03 * rs.rand(); w = 0.04 + 0.012 * rs.rand()
        pts = [base + a * w + b * w * 0.5, base - a * w + b * w * 0.4, base + a * w * 0.3 - b * w * 0.7,
               base - a * w * 0.6 - b * w * 0.5, base + d * h + a * w * 0.15, base + d * h * 0.7 - a * w * 0.5,
               base - d * 0.02]
        k.add(f"shard{i}", hull(pts), ROCK if i % 2 else ROCK_L, "head", fuse=False, surface="shell")

    # the mouth under the snout's root, a set line
    M, B = srgb(ct.INK), srgb(PINK)
    def line(u, v):
        c = np.tile(B, (len(u), 1))
        c[(np.abs(v + 0.1 * u * u) < 0.32) & (np.abs(u) < 0.88)] = M
        return c
    k.decal((0, -0.27, 0.528), unit(0, -0.55, -0.83), (0.03, 0.01), line, eps=0.0025, rings=8, segs=40)

    k.build(voxel=0.007, smooth=4, band=0.008)

    # small scowling eyes under the mound's brim: a pale almond, a dark pupil, the lid slanting to the snout
    E = np.array([0.07, -0.248, 0.625])
    n = unit(0.5, -0.8, 0.3)
    G, P, L = srgb(EYE_GREY), srgb(PUPIL), srgb(BLACK)
    def open_look(u, v):
        c = np.tile(G, (len(u), 1))
        c[(u + 0.05) ** 2 + (v + 0.15) ** 2 < 0.5 ** 2] = P
        c[v > 0.3 + 0.35 * u] = L                         # the scowl: the lid low toward the snout (u inward)
        return c
    def eyes(state):
        if state in ("Open", "Fierce"):
            look = open_look if state == "Open" else (lambda u, v: _fiercer(open_look(u, v), u, v, L))
        else:
            look = eye_look(state, open_look, L, srgb(ct.LASH), line=0.2)
        k.decal(E, n, (0.024, 0.016), look, eps=0.003, rings=20, segs=56, mirror=True)
    k.eye_states(eyes)
    k.export(out)


def _fiercer(c, u, v, lid):
    c = c.copy()
    c[v > 0.05 + 0.55 * u] = lid
    return c


if __name__ == "__main__":
    build(*args())
