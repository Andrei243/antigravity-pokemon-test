# Crocalor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crocalor.py -- <the export's Crocalor.glb> <out.glb>
# Fuecoco grown (built by fuecoco.py's functions: the line's red, cream and yellow, its dome and jaws, carved mouth,
# teeth, claws and limbs): a red crocodile leaning forward on short thick legs with black feet, its great cream head
# open in a wide grin edged with rows of white sawteeth above and below, pink within; a red triangle on the snout's
# top and a red band up each cheek; round white eyes with black pupils on bumps on the head's top; on the crown a
# wide wreath of orange blocks of flame each set with a yellow square, a yellow egg of fire rising from its back; a
# cream chest with two black bars down it like braces; a yellow plate over the back crossed by red bands; short red
# arms held forward with black three-clawed hands; a short tail that curls up at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from types import SimpleNamespace
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, smoothstep, unit, polygon, args, sibling

F = sibling("fuecoco")
RED, CREAM, YELLOW = "#D9402E", F.CREAM, "#EBC94A"
BLACK = "#24232A"
ORANGE, GLOW_ORANGE = "#F5872A", "#FF8A2A"
GOLD, GLOW_GOLD = "#FFD84A", "#FFD84A"
VOXEL = 0.0056

L = SimpleNamespace(**vars(F.L))
L.H, L.HR, L.dome_box = np.array([0.0, -0.035, 0.395]), np.array([0.135, 0.12, 0.085]), 2.3
L.dome_col = CREAM
L.hinge = (0.0, -0.085, 0.345)
L.gape_up, L.gape_down = 16, 30
L.upper_len, L.upper_r, L.upper_tip, L.upper_col = 0.2, (0.11, 0, 0.04), 0.74, CREAM
L.lower_len, L.lower_r, L.lower_tip, L.lower_col = 0.18, (0.104, 0, 0.036), 0.76, CREAM
L.maw_w, L.crease, L.lip, L.tongue_drop = 0.08, 0.016, 0.009, -0.008
L.tooth_seat = 0.004
# Rows of sawteeth: (t along the jaw, x, length, width, blunt)
L.upper_teeth = [(t, 0.088 * (1 - 0.3 * t ** 1.5), 0.022, 0.02, 0.0) for t in (0.42, 0.58, 0.73, 0.87)]
L.lower_teeth = [(t, 0.083 * (1 - 0.3 * t ** 1.5), 0.022, 0.02, 0.0) for t in (0.4, 0.56, 0.71, 0.85)]
L.eye_dir, L.eye_shift, L.eye_h = (0.42, -0.74, 0.52), (0, 0, 0.004), (0.024, 0.026)
L.lid, L.glint = CREAM, (-0.3, 0.4, 0.26)


def eye_open():
    """Crocalor's open eye: white with a big round black pupil and a black rim."""
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(np.array([1.0, 1.0, 1.0]), (len(u), 1))
        c[np.sqrt((u + 0.05) ** 2 + (v + 0.02) ** 2) < 0.46] = F.INK
        c[r >= 0.8] = F.INK
        return c
    return look


L.eye_look = eye_open()


def block(c, size, turn):
    """A rounded-off cube of flame: a hull of a cube with its corners cut, turned by turn (degrees about z, x)."""
    a = np.radians(turn[0]); b = np.radians(turn[1])
    Rz = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]])
    Rx = np.array([[1, 0, 0], [0, math.cos(b), -math.sin(b)], [0, math.sin(b), math.cos(b)]])
    pts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                for k in range(3):
                    p = np.array([sx, sy, sz], float)
                    p[k] *= 0.6
                    pts.append(p * size / 2)
    P = np.array(pts) @ (Rz @ Rx).T + np.array(c, float)
    return hull(P)


def crown(k, top):
    """The wreath of flame on the crown: orange blocks in a ring wider than the head, each with a yellow square
    standing out of its top-outer face, a filling of blocks within, and a yellow egg rising behind."""
    oranges, golds = [], []
    ring = 10
    for j in range(ring):
        a = 2 * math.pi * (j + 0.5) / ring
        d = np.array([math.sin(a), -math.cos(a), 0.0])
        r = 0.115 + 0.012 * math.sin(3 * a)
        c = top + d * r + np.array([0, 0, 0.012 + 0.008 * math.cos(2 * a)])
        size = 0.062 + 0.008 * math.sin(5 * a)
        oranges.append(block(c, size, (math.degrees(a) + 12 * math.sin(4 * a), 18 * math.cos(3 * a))))
        g = c + d * size * 0.33 + np.array([0, 0, size * 0.28])
        golds.append(block(g, size * 0.48, (math.degrees(a) + 20, 30)))
    for j, (x, y) in enumerate([(0.0, -0.03), (0.05, 0.03), (-0.05, 0.03), (0.0, 0.07)]):
        oranges.append(block(top + np.array([x, y, 0.026]), 0.07, (25 * j, 10)))
    k.add("crown", F.join(*oranges), ORANGE, "head", fuse=False, glow=GLOW_ORANGE)
    k.add("crowngold", F.join(*golds), GOLD, "head", fuse=False, glow=GLOW_GOLD)
    egg = ell(top + np.array([0.0, 0.055, 0.075]), (0.042, 0.04, 0.05), rot=(-25, 0, 0))
    k.add("egg", egg, GOLD, "head", fuse=False, glow=GLOW_GOLD)


def crocalor(src, out):
    k = Kit(src, coat="scales")

    # The body: a pear leaning forward, round in the belly, low over the thick legs
    def pear(V):
        V = V.copy()
        low = np.clip(-V[:, 2], 0, 1)
        V[:, :2] *= (1 + 0.12 * low)[:, None]
        return V
    k.add("torso", ell((0, 0.04, 0.2), (0.138, 0.14, 0.14), rot=(-26, 0, 0), deform=pear), RED, "body")
    k.add("hips", ell((0, 0.06, 0.12), (0.132, 0.12, 0.075)), RED, "body")
    k.add("neck", ell((0, -0.05, 0.3), (0.11, 0.09, 0.075)), RED, "body")

    # The legs: fat haunches fused into the hips, broad black feet with black claws
    for s, side in ((1, "R"), (-1, "L")):
        F.leg(k, side, s, (0.088, 0.02, 0.075), (0.074, 0.085, 0.07), (0.092, -0.01, 0.022), (0.056, 0.07, 0.024),
              [(-0.032, 0.03, 0.022, (-0.2, -1, -0.4)), (0.0, 0.032, 0.024, (0, -1, -0.4)),
               (0.032, 0.03, 0.022, (0.2, -1, -0.4))], color=RED, claw_col=BLACK, foot_col=BLACK)

    # The arms: short, forward and a little out and down, black hands with three black claws
    for s, side in ((1, "R"), (-1, "L")):
        F.arm(k, side, s, (0.09, -0.05, 0.27), (0.14, -0.18, 0.22), [0.036, 0.031, 0.028], (0.03, 0.03, 0.026),
              color=RED, paw_col=BLACK, claws=(0.026, 0.017, 0.35, (0.0, -1, -0.35)), claw_col=BLACK)

    # The tail: thick at the hips, back and low, curling up at its end out to the model's +X side
    TAIL = np.array([(0, 0.13, 0.17), (0.02, 0.22, 0.11), (0.06, 0.3, 0.075), (0.1, 0.345, 0.085),
                     (0.115, 0.355, 0.12), (0.1, 0.34, 0.145)])
    TAIL_R = [0.08, 0.06, 0.042, 0.03, 0.024, 0.02]
    k.add("tail", tube(TAIL, TAIL_R), RED, "tail")

    # The cream chest under the jaw, down to the legs
    def chest(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(y - (-0.045 + 0.25 * (z - 0.25)), 0.003) * inside(np.abs(x) - 0.095, 0.004) \
            * inside(z - 0.34, 0.003) * inside(0.11 - z, 0.003)
    k.paint(chest, CREAM, on=["torso", "hips", "neck"])

    # The yellow plate over the back and down the tail's root, crossed by red bands
    def plate(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        back = inside(0.035 + 0.12 * (0.34 - z) - y, 0.003) * inside(np.abs(x) - 0.1, 0.004) * inside(z - 0.36, 0.003)
        return back * inside(0.15 - z + 0.4 * np.clip(y - 0.13, 0, None), 0.003)
    k.paint(plate, YELLOW, on=["torso", "hips", "tail", "neck"])

    def bands(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        t = z + 0.55 * np.abs(x)
        return plate(P) * (inside(np.abs(t - 0.31) - 0.011, 0.002) + inside(np.abs(t - 0.23) - 0.011, 0.002))
    k.paint(bands, RED, on=["torso", "hips", "tail", "neck"])

    # The head, its open mouth with rows of sawteeth
    J = F.head(k, L)
    F.teeth(k, L, J)

    # Eye bumps on the head's top
    E, de, right, up = F.eye_frame(L)
    k.add("eyebump", ell(E - de * 0.016, (0.032, 0.03, 0.03)), CREAM, "head", mirror=True)

    # The red marks: a triangle on the snout's top, a band up each cheek
    tip = J.hinge + J.du * L.upper_len * 0.78 + J.upu * L.upper_r[2] * 1.05
    k.decal(tip, J.upu + J.du * 0.2, (0.03, 0.026), RED, up=J.du, eps=0.0018, rings=8, segs=40,
            outline=polygon([(-0.95, -0.7), (0.95, -0.7), (0.0, 1.0)]))
    cheek = np.array([0.122, -0.06, 0.37])
    k.decal(cheek, (1, -0.25, 0.0), (0.03, 0.026), RED, up=(0, 0, 1), eps=0.0018, rings=8, segs=40, mirror=True,
            outline=polygon([(-1, -0.9), (0.6, -0.9), (1, 0.9), (-0.6, 0.9)]))

    # The black bars down the chest, like braces
    # (cast level from the front, below the lower jaw, which would catch them higher up)
    for s in (1, -1):
        k.decal((0.034 * s, -0.11, 0.172), (0.0, -1, 0.0), (0.012, 0.046), BLACK, eps=0.0018, rings=8, segs=40,
                outline=polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)]))

    # The crown of flame
    crown(k, F.on_ell(L.H, L.HR, (0, 0.45, 1)) + np.array([0, 0.015, 0.012]))

    lay = F.eyes(k, L)
    k.build(voxel=VOXEL, smooth=4, band=0.006, wband=0.05)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    crocalor(*args())
