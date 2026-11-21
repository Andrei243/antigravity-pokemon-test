# Scovillain-Mega sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/scovillainmega.py -- <the export's Scovillain-Mega.glb> <out.glb>
#
# Grown from Scovillain (sibling("scovillain")): its two gaping pepper heads (PepperHead), their sly angry eyes, its
# solid leaf points and its greens. From the references (they overrule the C# sculptor's two flame tongues): the body
# becomes a crouching dark green beast, a great low haunch on two thick short legs with cream claws and a cream slit
# mark on each thigh, and a long thick tail behind whose end rises with cream spikes (green ones nearer the body); the
# chest is a ragged black ruff of thorny blades with a thin black branch of an arm reaching out on the left; the red
# head stands over the middle on a short black neck, a quarter larger than the green, crowned with tall cream horns
# and small black thorns at its cheeks; the green head is held out to the right and forward on a long black branch of
# a neck, cream horns round the back of its head and a forked orange tongue hanging from its gaping jaw; a jagged red
# bolt of flame hangs from under the ruff down the front of the body to the ground.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, slab, args, sibling

sco = sibling("scovillain")
cap = sco.cap

RED, HGREEN, DARKB = sco.RED, sco.HGREEN, "#2E6E45"
BLACK, CREAM, BOLT, TONGUE = "#25232B", "#F0EBD2", "#E0402A", "#E8923A"
VOXEL = 0.0062


def on_ell(C, R, d, lift=0.0):
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(R)) ** 2).sum())
    return np.array(C, float) + d * (s + lift)


class MegaHead(sco.PepperHead):
    """The base's pepper head, a size of its own, its eyes a colour of their own, with horns and thorns added."""

    def __init__(self, k, tag, bone, centre, look, colour, neck_from, K, pupil):
        self.K = K
        self.pupil = pupil
        sco.STALK = BLACK                  # the necks are black branches now (read as the head is made)
        super().__init__(k, tag, bone, centre, look, colour, neck_from)

    def at(self, p):
        return self.c + self.M @ (np.array(p, float) * self.K)

    def point(self, name, p, d, width, length, thick, colour, surface="shell"):
        cap.point(self.k, f"{name}{self.tag}", self.at(p), self.M @ unit(*d), width * self.K, length * self.K,
                  thick * self.K, colour, self.bone, up=self.M[:, 0], surface=surface)

    def eyes(self, state):
        lk = eye_look(state, cap.angry_eye(self.pupil), srgb(self.colour), srgb(cap.INK), line=0.16)
        r = cap.cut_outline(0.22, 0.45)
        up = self.M[:, 2]
        for p, n, o in self.eyes_at:
            right = np.cross(up, n)
            sg = 1.0 if right @ o > 0 else -1.0
            self.k.decal(p, n, (0.03 * self.K, 0.027 * self.K), (lambda u, v, sg=sg: lk(sg * u, v)), up=up,
                         eps=0.0022, outline=(r if sg > 0 else (lambda th: r(math.pi - th))), rings=20, segs=56)


def red_head(k):
    h = MegaHead(k, "L", "headL", (-0.035, -0.02, 0.615), (-0.12, -1.0, 0.02), RED, (-0.012, 0.0, 0.43), 1.12,
                 "#C2602A")
    # the crown of tall cream horns standing up behind the top, the middle one tallest
    for x, lean, ln in ((-0.075, -0.45, 0.085), (-0.03, -0.15, 0.13), (0.03, 0.15, 0.13), (0.075, 0.45, 0.085)):
        h.point(f"horn{int(x * 1000)}", (x, 0.035, 0.09), (lean, 0.35, 1.0), 0.026, ln, 0.022, CREAM)
    # small black thorns at the cheeks
    for s in (1, -1):
        for j, (z, dz) in enumerate(((0.0, 0.2), (-0.06, -0.25))):
            h.point(f"thorn{'R' if s > 0 else 'L'}{j}", (0.105 * s, 0.01, z), (s, 0.1, dz), 0.014, 0.05, 0.013,
                    BLACK, surface="leaf")
    return h


def green_head(k):
    h = MegaHead(k, "R", "headR", (0.3, -0.09, 0.47), (0.7, -0.7, -0.08), HGREEN, (0.06, -0.03, 0.44), 1.05,
                 "#A6A83A")
    # cream horns round the back of the head
    for i, (p, d, ln) in enumerate((((0.0, 0.05, 0.1), (0.0, 0.5, 1.0), 0.11), ((-0.08, 0.06, 0.07), (-0.7, 0.5, 0.6), 0.09),
                                    ((0.08, 0.06, 0.07), (0.7, 0.5, 0.6), 0.09), ((0.0, 0.1, 0.0), (0.0, 1.0, 0.15), 0.1))):
        h.point(f"horn{i}", p, d, 0.032, ln, 0.026, CREAM)
    # a forked orange tongue hanging from the gaping jaw
    m = h.at((0.0, -0.05, -0.07))
    pts = [m, m + np.array([0.02, -0.03, -0.05]), m + np.array([-0.005, -0.04, -0.1]), m + np.array([0.02, -0.045, -0.15])]
    k.add("gtongue", outward(tube(pts, [0.016, 0.014, 0.014, 0.013])), TONGUE, "headR", fuse=False, surface="skin")
    for s in (1, -1):
        e = pts[-1] + np.array([0.035 * s, -0.01, -0.035])
        k.add(f"gfork{'R' if s > 0 else 'L'}", outward(tube([pts[-1], e], [0.013, 0.013])), TONGUE, "headR",
              fuse=False, surface="skin")
    return h


def body(k):
    # the great low haunch, longer than wide, its belly a few hundredths off the ground
    k.add("bulb", ell((0, 0.04, 0.2), (0.15, 0.175, 0.13)), DARKB, "body")
    k.add("hip", ell((0, 0.1, 0.17), (0.13, 0.13, 0.11)), DARKB, "body")
    # thick short legs: haunches fused into the flanks, a stub of shin, a broad foot
    k.add("thigh", ell((0.125, -0.01, 0.145), (0.085, 0.11, 0.1), rot=(0, -10, 0)), DARKB, "leg{s}", mirror=True)
    k.add("shin", tube([(0.14, -0.02, 0.12), (0.155, -0.035, 0.05)], [0.062, 0.055]), DARKB, "leg{s}", mirror=True)
    k.add("foot", ell((0.158, -0.05, 0.033), (0.066, 0.085, 0.034)), DARKB, "leg{s}", mirror=True)
    for s in (1, -1):        # cream claws, two to a foot
        for j, dx in enumerate((-0.03, 0.03)):
            cap.point(k, f"claw{'R' if s > 0 else 'L'}{j}", (s * (0.158 + dx), -0.115, 0.03), unit(0.15 * dx * s, -1, -0.15),
                      0.02, 0.045, 0.026, CREAM, f"leg{'R' if s > 0 else 'L'}", surface="shell")
    # the long thick tail, its end rising
    tp = [(0, 0.17, 0.18), (0.06, 0.3, 0.12), (0.16, 0.39, 0.13), (0.24, 0.45, 0.2), (0.27, 0.47, 0.28)]
    k.add("tail", tube(tp, [0.085, 0.064, 0.047, 0.034, 0.024]), DARKB, "tail")
    T = np.array(tp)
    def along(t):            # a point of the tail's line, t from 0 to 4
        i = min(int(t), 3); f = t - i
        return T[i] * (1 - f) + T[i + 1] * f
    for i, (t, lift, d, ln, w, col) in enumerate(((0.55, 0.075, (0, 0.3, 1), 0.07, 0.03, DARKB),
                                                  (1.35, 0.05, (0.1, 0.3, 1), 0.07, 0.028, DARKB),
                                                  (2.1, 0.04, (0.25, 0.2, 1), 0.085, 0.03, CREAM),
                                                  (2.9, 0.03, (0.35, 0.3, 1), 0.08, 0.028, CREAM),
                                                  (3.7, 0.02, (0.3, 0.2, 1), 0.07, 0.022, CREAM),
                                                  (3.9, 0.0, (0.8, 0.7, 0.3), 0.055, 0.02, CREAM))):
        p = along(t) + np.array([0, 0, lift])
        tdir = T[min(int(t), 3) + 1] - T[min(int(t), 3)]
        cap.point(k, f"tspike{i}", p, d, w, ln, 0.024, col, "tail", up=tuple(np.cross(tdir, (0, 0, 1.0))),
                  surface="shell" if col == CREAM else "leaf")
    # two green points along the back
    for i, (p, ln) in enumerate((((0.0, 0.08, 0.32), 0.07), ((0.0, 0.17, 0.3), 0.06))):
        cap.point(k, f"bspike{i}", p, (0, 0.5, 1), 0.03, ln, 0.024, DARKB, "body", up=(1, 0, 0))
    # the cream slit mark on each thigh
    for s in (1, -1):
        c = on_ell((0.125 * s, -0.01, 0.145), (0.085, 0.11, 0.1), (0.75 * s, -0.66, 0.1))
        n = unit(0.75 * s, -0.66, 0.1)
        def mark(u, v):
            col = np.tile(srgb(CREAM), (len(u), 1))
            col[(np.abs(u - 0.15 * v) < 0.2) & (np.abs(v) < 0.62)] = srgb(DARKB)
            return col
        k.decal(c, n, (0.026, 0.05), mark, up=(0, 0, 1), eps=0.002, rings=16, segs=48)
    # the black chest under its ragged ruff
    k.add("chest", ell((0, -0.015, 0.37), (0.1, 0.09, 0.1)), BLACK, "body")
    rs = np.random.RandomState(48)
    for i in range(11):
        t = math.radians(i * 360 / 11 + 10)
        d = np.array([math.sin(t), -math.cos(t), 0.0])
        root = np.array([0, -0.01, 0.43]) + d * 0.06
        ln = 0.1 + 0.06 * rs.rand()
        cap.point(k, f"ruff{i}", root, unit(*(d + np.array([0, 0, -0.55 + 0.3 * rs.rand()]))), 0.035, ln, 0.022,
                  BLACK, "body")
    # a thin black branch of an arm reaching out on the left, held forward and a little down, twigs at its end
    arm = [(-0.07, -0.03, 0.42), (-0.15, -0.08, 0.42), (-0.21, -0.14, 0.375)]
    k.add("arm", tube(arm, [0.02, 0.016, 0.014]), BLACK, "body")
    for i, e in enumerate(((-0.25, -0.2, 0.39), (-0.26, -0.13, 0.33), (-0.19, -0.11, 0.47))):
        k.add(f"twig{i}", outward(tube([arm[-1] if i < 2 else arm[1], e], [0.014, 0.013])), BLACK, "body",
              fuse=False, surface="leaf")
    # the jagged red bolt of flame hanging from under the ruff to the ground
    bolt = [(0, -0.02), (0.1, 0.028), (0.16, -0.01), (0.27, 0.036), (0.34, 0.0), (0.4, 0.008),
            (0.29, -0.012), (0.23, -0.05), (0.12, -0.02), (0.06, -0.055)]
    k.add("bolt", slab(bolt, (0.075, -0.115, 0.39), unit(0.18, -0.22, -1), unit(1, 0.3, 0), 0.024), BOLT, "body",
          fuse=False, glow=BOLT, surface="leaf")


def scovillainmega(src, out_path):
    k = Kit(src, coat="leaf")
    body(k)
    heads = [red_head(k), green_head(k)]
    k.build(voxel=VOXEL, smooth=3, band=0.007, wband=0.045)
    k.eye_states(lambda state: [h.eyes(state) for h in heads])
    k.export(out_path)


if __name__ == "__main__":
    scovillainmega(*args())
