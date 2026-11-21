# Turtonator sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/turtonator.py -- <the export's Turtonator.glb> <out.glb>
# From the references: a turtle standing upright on two short legs under a great shell on its back, the largest mass
# of it: a coral-red dome whose rim is a ring of points, a broad yellow star over its back crossed by red rays from the
# middle, and long dark brown blades bursting out of it. A pale sage body, a thick neck carrying the head forward; on
# the head a flat red plate wider than the head, its edge a ring of points, a yellow star on it with a brown heart,
# tilted forward over the face; white eyes with a red iris framed in yellow under the plate's edge, a dark hooked beak
# edged in yellow. Arms bent forward and down, each ending in a curved hook; short thick legs in red with a zigzag at
# their tops, broad red feet with white claws; a red tail of points trailing behind on the ground.
# The pictures overrule the task's text: the belly is sage, not red; the neck is short and thick; the yellow rims the
# eyes and the beak; the legs and tail are red.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, tri, unit, polygon, eye_look, args, outward

SAGE, BELLY = "#BCBBA4", "#D4D2BE"
RED, YELLOW, BROWN = "#EE5660", "#F6D82E", "#5C2C24"
CLAW, BEAK = "#F0EEE2", "#3A2824"
INK, WHITE = srgb("#18141A"), np.array([1.0, 1.0, 1.0])
IRIS = srgb("#B02436")
VOXEL = 0.0062

SC = np.array([0.0, 0.075, 0.335])           # the shell's middle
SR = np.array([0.215, 0.135, 0.225])
SHELL_TURN = 25                               # degrees the shell is turned out to the +X side
H = np.array([0.0, -0.13, 0.5])               # the head
HR = np.array([0.064, 0.066, 0.058])


def on_ell(c, r, d):
    d = unit(*d)
    return np.array(c, float) + d / math.sqrt(((d / np.array(r, float)) ** 2).sum())


def normal_on(c, r, d):
    p = on_ell(c, r, d) - np.array(c, float)
    n = p / np.array(r, float) ** 2
    return n / np.linalg.norm(n)


def blade(base, d, length, width, thick, flat):
    """A long dark blade: a flat-faced kite at its root (width across `flat`, thick across that) to a point along d,
    widest a third of the way."""
    d = unit(*d)
    a = np.array(flat, float); a = a - d * (a @ d); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    base = np.array(base, float)
    mid = base + d * length * 0.32
    return hull([base + a * width * 0.35, base - a * width * 0.35, base + b * thick / 2, base - b * thick / 2,
                 mid + a * width / 2, mid - a * width / 2, mid + b * thick * 0.4, mid - b * thick * 0.4,
                 base + d * length])


def point(base, d, length, width, thick, up):
    """A short pointed tooth of the rim: a kite at its root to a point along d."""
    return blade(base, d, length, width, thick, up)


def star(n, inner, squash=1.0):
    """An n-pointed star's outline (points at radius 1, notches at `inner`), the first point straight up."""
    pts = []
    for i in range(2 * n):
        th = math.pi / 2 + math.pi * i / n
        r = 1.0 if i % 2 == 0 else inner
        pts.append((r * math.cos(th) * squash, r * math.sin(th)))
    return pts


def turtonator(src, out):
    k = Kit(src, coat="scales")

    # The body: a sage barrel, its front paler, leaning a little forward
    k.add("torso", ell((0, -0.015, 0.31), (0.125, 0.105, 0.15), rot=(8, 0, 0)), SAGE, "body")
    k.add("hips", ell((0, 0.0, 0.215), (0.13, 0.1, 0.08)), SAGE, "body")
    k.decal((0, -0.118, 0.3), unit(0, -1, 0.08), (0.085, 0.12), BELLY, eps=0.003, rings=16, segs=56)

    # The shell on the back: a coral dome, the rim a ring of points. Its face is turned SHELL_TURN degrees out to the
    # +X side, so that both of the game's cameras see the star and the blades rather than an edge.
    a = math.radians(-SHELL_TURN)
    RZ = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1.0]])

    def at(p):
        return SC + RZ @ (np.array(p, float) - SC)

    def way(d):
        return RZ @ np.array(d, float)
    k.add("shell", ell(SC, SR, rot=(0, 0, -SHELL_TURN)), RED, "body", surface="shell")
    for i in range(14):
        th = 2 * math.pi * i / 14 + 0.1
        d = np.array([math.cos(th), 0.0, math.sin(th)])
        base = SC + np.array([SR[0] * d[0], 0.01, SR[2] * d[2]]) * 0.92
        if base[2] < 0.13:
            continue
        k.add(f"rim{i}", point(at(base), way(d + np.array([0, 0.25, 0])), 0.06, 0.07, 0.04, way((0, 1, 0))), RED,
              "body", fuse=False, surface="shell")
    # the star over the back, crossed by red rays, and the brown blades bursting out of it
    back, bn = at(on_ell(SC, SR, (0, 1, 0))), way((0, 1, 0))
    k.decal(back, bn, (0.2, 0.21), YELLOW, eps=0.003, rings=24, segs=96, outline=polygon(star(8, 0.62)),
            up=(0, 0, 1))
    cross = []
    for i in range(8):
        th = math.pi / 4 + math.pi / 4 * i
        r = 0.95 if i % 2 == 0 else 0.1
        cross.append((r * math.cos(th), r * math.sin(th)))
    k.decal(back, bn, (0.2, 0.21), RED, eps=0.0045, rings=24, segs=128, outline=polygon(cross), up=(0, 0, 1))
    for i in range(8):
        th = math.pi / 8 + math.pi / 4 * i
        rad = 0.45 if i % 2 == 0 else 0.6
        d = (math.cos(th) * rad * 1.4, 1.0, math.sin(th) * rad * 1.4)
        base = on_ell(SC, SR, d)
        n = normal_on(SC, SR, d)
        out_dir = n * 0.5 + np.array([math.cos(th), 0, math.sin(th)]) * 0.9
        k.add(f"blade{i}", blade(at(base - n * 0.025), way(out_dir), 0.23 if i % 2 == 0 else 0.18, 0.07, 0.032,
                                 way((-math.sin(th), 0, math.cos(th)))), BROWN, "body", fuse=False, surface="shell")

    # The neck and head: a thick neck carrying the head forward
    k.add("neck", tube([(0, -0.03, 0.4), (0, -0.07, 0.45), (0, -0.105, 0.48)], [0.058, 0.054, 0.05]), SAGE, "head")
    k.add("head", ell(H, HR), SAGE, "head")
    k.add("cheeks", ell(H + (0, -0.02, -0.02), (0.06, 0.05, 0.04)), SAGE, "head")
    # The beak: dark and hooked, edged in yellow
    k.add("beakrim", hull([H + (0.034, -0.045, -0.002), H + (-0.034, -0.045, -0.002), H + (0, -0.05, 0.03),
                           H + (0.026, -0.04, -0.045), H + (-0.026, -0.04, -0.045), H + (0, -0.095, -0.012),
                           H + (0, -0.085, -0.062)]), YELLOW, "head", fuse=False)
    k.add("beak", hull([H + (0.026, -0.058, -0.006), H + (-0.026, -0.058, -0.006), H + (0, -0.062, 0.022),
                        H + (0.019, -0.054, -0.04), H + (-0.019, -0.054, -0.04), H + (0, -0.104, -0.014),
                        H + (0, -0.096, -0.068)]), BEAK, "head", fuse=False)
    # The plate on the head: red, wider than the head, tilted forward, a yellow star on it with a brown heart
    pc = H + np.array([0.0, 0.02, 0.07])
    ex = np.array([1.0, 0, 0])
    ez = unit(0, 0.96, 0.28)                    # the plate runs back and up: its top face (nrm) tilts to the front
    rimpts = []
    for i in range(16):
        th = 2 * math.pi * i / 16
        r = 1.0 if i % 2 == 0 else 0.78
        rimpts.append((r * math.cos(th) * 0.13, r * math.sin(th) * 0.088))
    nrm = np.cross(ex, ez)
    k.add("plate", slab(rimpts, pc, ex, ez, 0.022), RED, "head", fuse=False, surface="shell")
    k.add("platestar", slab([(p[0] * 0.08, p[1] * 0.065) for p in star(4, 0.38, 1.0)], pc + nrm * 0.006, ex, ez,
                            0.014), YELLOW, "head", fuse=False, surface="shell")
    k.add("plateheart", slab([(p[0] * 0.028, p[1] * 0.024) for p in star(4, 0.45, 1.0)], pc + nrm * 0.011, ex, ez,
                             0.012), BROWN, "head", fuse=False, surface="shell")

    # The arms: forward and down, each ending in a curved hook
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"arm{side}", tube(np.array([(0.11, -0.03, 0.39), (0.15, -0.06, 0.32), (0.12, -0.13, 0.27),
                                            (0.095, -0.165, 0.29), (0.085, -0.17, 0.325)]) * m,
                                  [0.04, 0.034, 0.028, 0.022, 0.016]), SAGE, f"arm{side}")

    # The legs: short and thick, red with a zigzag at their tops, broad feet with white claws
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        k.add(f"thigh{side}", ell(np.array((0.09, -0.005, 0.165)) * m, (0.07, 0.075, 0.075)), RED, f"leg{side}")
        k.add(f"shin{side}", tube(np.array([(0.1, -0.01, 0.13), (0.105, -0.02, 0.06)]) * m, [0.062, 0.058]), RED,
              f"leg{side}")
        k.add(f"foot{side}", ell(np.array((0.105, -0.045, 0.032)) * m, (0.068, 0.085, 0.036)), RED, f"leg{side}")
        for j, dx in enumerate((-0.035, 0.0, 0.035)):
            b = np.array((0.105 + dx, -0.118, 0.022))
            k.add(f"claw{side}{j}", blade(b * m, np.array((dx * 0.8, -1, -0.3)) * m, 0.04, 0.03, 0.022, (1, 0, 0)),
                  CLAW, f"leg{side}", fuse=False, surface="shell")
    # the zigzag where the red legs meet the sage body
    def legs(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        cx = np.sign(x) * 0.095
        ang = np.arctan2(y, x - cx)
        top = 0.2 + 0.022 * tri(ang * 6 / (2 * math.pi))
        return inside(z - top, 0.002) * inside(0.045 - np.abs(x), 0.004) * (z < 0.24)
    k.paint(legs, RED, on=["thigh", "shin", "foot", "hips", "torso"])

    # The tail: red, trailing behind on the ground and out to the +X side, three points along its top
    TAIL = np.array([(0, 0.1, 0.18), (0.02, 0.2, 0.09), (0.05, 0.29, 0.04)])
    k.add("tail", tube(TAIL, [0.05, 0.035, 0.022]), RED, "tail", surface="shell")
    for j, t in enumerate((0.25, 0.55, 0.85)):
        p = TAIL[0] + (TAIL[-1] - TAIL[0]) * t
        k.add(f"tailpoint{j}", point(p + (0, 0, 0.03 - 0.02 * t), (0.1, 0.6, 1.0), 0.045 - 0.012 * t, 0.04, 0.03,
                                     (0, 1, 0)), RED, "tail", fuse=False, surface="shell")

    # The eyes: white with a red iris and a dark pupil, framed in yellow, the plate's edge over them
    de = unit(0.5, -0.86, 0.1)
    E = on_ell(H, HR, de) + np.array([0, 0, 0.004])

    def open_eye(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(srgb(YELLOW), (len(u), 1))
        c[r < 0.78] = INK
        c[r < 0.66] = WHITE
        c[((u + 0.12) / 0.42) ** 2 + ((v + 0.05) / 0.48) ** 2 < 1] = IRIS
        c[((u + 0.14) / 0.2) ** 2 + ((v + 0.06) / 0.24) ** 2 < 1] = INK
        c[v > 0.42 - 0.25 * u] = srgb(SAGE)         # a heavy brow over it, lower toward the beak
        c[np.abs(v - (0.42 - 0.25 * u)) < 0.13] = INK
        c[r >= 0.86] = srgb(YELLOW)
        return c
    LID = srgb(SAGE)

    def eyes(state):
        k.decal(E, de, (0.026, 0.026), eye_look(state, open_eye, LID, INK, line=0.16), eps=0.003, rings=20, segs=56,
                mirror=True)

    k.build(voxel=VOXEL, smooth=4, band=0.0028, wband=0.05)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    turtonator(*args())
