# Fearow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/fearow.py -- <the export's Fearow.glb> <out.glb>
# Spearow grown (spearow.py, whose feather points, beak, feet and cut eyes it borrows): a big lean brown bird, its
# broad wings raised in a V and swept back with pale cream ends to the long feathers, a long bare neck carried in an S
# out of a shaggy cream collar, a small head with a fan of red feathers on its crown, a long thin pink beak hanging
# open, a fan of brown tail feathers, and short pink legs with white claws.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, lumpy, sibling, args

sp = sibling("spearow")

BROWN, DARK, CREAM, COLLAR, RED = "#B4743E", "#8A5530", "#F1E0B8", "#E2D4B4", "#E0544A"
BEAK = "#EDB6C2"
H = np.array([0, -0.235, 0.735])          # the head's centre
HR = np.array([0.09, 0.104, 0.087])


def wing_frame():
    """The +X wing's frame: span raised up and out and swept back, chord toward its trailing, outer, lower edge."""
    ex = unit(0.35, 0.35, 0.87)
    ez = unit(0.85, 0.4, -0.3)
    ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    return ex, ez


SPAN, CHORD, FINGERS = 0.3, 0.29, 6         # the wing's arm, its breadth at the far end, its long feathers


def wing_arm():
    """The wing's broad inner part in (along the span, across the chord), from the shoulder to where the fingers start."""
    return [(0.0, -0.035), (SPAN * 0.5, -0.04), (SPAN - 0.01, -0.03), (SPAN + 0.01, 0.05), (SPAN, 0.17),
            (SPAN - 0.03, 0.25), (SPAN * 0.65, CHORD - 0.02), (0.12, CHORD * 0.62), (0.0, 0.06)]


def coverts():
    """The darker band of covert feathers near the body, its outer edge in scallops."""
    return [(0.02, -0.035), (0.2, -0.035), (0.23, 0.02), (0.195, 0.06), (0.225, 0.11), (0.185, 0.155), (0.2, 0.2),
            (0.14, 0.2), (0.1, CHORD * 0.6 - 0.02), (0.02, 0.06)]


def finger(j, tip_from=None, seg=10):
    """
    Long finger feather j (0 the leading one) in the wing's plane: from inside the arm out past it, its end rounded
    into a scallop; tip_from cuts it to its end from that far along (the cream end).
    """
    w = CHORD / FINGERS
    b0 = -0.03 + j * w
    half = w * 0.62                          # overlapping its neighbours, a scallop each
    end = half * 0.7                         # tapering to a narrow rounded point
    mid = b0 + w / 2
    L = 0.2 - 0.06 * j / (FINGERS - 1)
    a0 = SPAN - 0.03
    a1 = SPAN + L - end
    hw = lambda a: half + (end - half) * (a - a0) / (a1 - a0)
    start = a0 if tip_from is None else a0 + tip_from * (a1 + end - a0)
    pts = [(start, mid - hw(start))]
    for t in np.linspace(-math.pi / 2, math.pi / 2, seg):
        pts.append((a1 + end * 1.4 * math.cos(t), mid + end * math.sin(t)))
    pts.append((start, mid + hw(start)))
    # the fingers fan out: each turned about its root, the trailing ones further toward the trailing edge
    phi = math.radians(-6 + 6 * j)
    c, s = math.cos(phi), math.sin(phi)
    return [(a0 + (a - a0) * c - (b - mid) * s, mid + (a - a0) * s + (b - mid) * c) for a, b in pts]


def fearow(src, out):
    k = Kit(src, coat="fur")

    # A lean trunk, long rather than round, its breast raised toward the neck, a little paler underneath
    def lean(V):
        V = V.copy(); t = np.clip(V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.3 * t; V[:, 2] *= 1 - 0.3 * t           # tapering to the tail
        return V
    k.add("body", ell((0, 0.05, 0.34), (0.112, 0.26, 0.112), rot=(-7, 0, 0), deform=lean), BROWN, "body")
    k.add("breast", ell((0, -0.08, 0.41), (0.1, 0.1, 0.1)), BROWN, "body")
    k.paint(lambda P: inside(P[:, 2] - 0.29, 0.004) * (P[:, 1] < 0.2), "#C88C56", on=["body"])

    # The shaggy cream collar where the neck leaves the body, large, and points of it standing out round its edge
    CC = np.array([0, -0.06, 0.5])
    k.add("collar", ell(CC, (0.14, 0.13, 0.085), deform=lumpy(7, 3, 0.18)), COLLAR, "body")
    for i, th in enumerate(np.linspace(-1.4, 1.4 + math.pi, 13)):
        for up, L in ((0.55, 0.11), (1.4, 0.08)):
            if up > 1 and i % 2:
                continue
            d = unit(math.sin(th), math.cos(th) * 0.9, up)
            base = CC + d * np.array([0.11, 0.11, 0.06])
            sp.feather(k, f"ruff{i}_{up}", base, base + d * L, 0.05, 0.02, COLLAR, "body")

    # The long bare neck in an S: up and forward, back, and the small head forward again
    k.add("neck", tube([(0, -0.06, 0.48), (0, -0.17, 0.53), (0, -0.2, 0.6), (0, -0.205, 0.67), H + (0, 0.025, -0.02)],
                       [0.05, 0.042, 0.038, 0.042, 0.05]), BROWN, "head")
    k.add("head", ell(H, HR), BROWN, "head")

    # The red fan of feathers on the crown, with width, leaning back
    for i, (d, L, w) in enumerate([((0, 0.2, 1), 0.13, 0.05), ((0.32, 0.3, 0.9), 0.11, 0.045), ((-0.32, 0.3, 0.9), 0.11, 0.045),
                                   ((0, 0.65, 0.75), 0.12, 0.05), ((0.3, 0.7, 0.6), 0.09, 0.04), ((-0.3, 0.7, 0.6), 0.09, 0.04),
                                   ((0, -0.3, 0.95), 0.08, 0.04)]):
        dd = unit(*d)
        base = H + dd * HR * 0.75
        sp.feather(k, f"comb{i}", base, base + dd * L, w, 0.018, RED, "head")

    # The long thin beak, drooping a little and open: the upper bill and a shorter lower one under it
    B = H + (0, -0.05, 0.005)                # the beak's root, in the face
    k.add("beak", tube([B, B + (0, -0.09, -0.02), B + (0, -0.18, -0.048), B + (0, -0.25, -0.077), B + (0, -0.275, -0.095)],
                       [0.03, 0.02, 0.013, 0.007, 0.002], seg=20), BEAK, "head", fuse=False, surface="shell")
    k.add("lowbill", tube([B + (0, 0.005, -0.025), B + (0, -0.06, -0.044), B + (0, -0.12, -0.064), B + (0, -0.16, -0.075)],
                          [0.02, 0.014, 0.009, 0.002], seg=16), BEAK, "head", fuse=False, surface="shell")

    # The wings, raised in a V and swept back: a brown plate, and cream ends on its long feathers thicker than it
    ex, ez = wing_frame()
    root = np.array([0.1, -0.03, 0.47])
    n = np.cross(ex, ez)
    k.add("wing", slab(wing_arm(), root, ex, ez, 0.024), BROWN, "wing{s}", fuse=False, mirror=True)
    k.add("coverts", slab(coverts(), root, ex, ez, 0.034), DARK, "wing{s}", fuse=False, mirror=True)
    for j in range(FINGERS):
        # each finger a hair off the arm's plane, alternately, so neighbours never share a face
        off = root + n * (0.003 if j % 2 else -0.003)
        k.add(f"finger{j}", slab(finger(j), off, ex, ez, 0.024), BROWN, "wing{s}", fuse=False, mirror=True)
        k.add(f"fingertip{j}", slab(finger(j, tip_from=0.58), off, ex, ez, 0.031), CREAM, "wing{s}", fuse=False,
              mirror=True)
    # the rounded leading edge, from the shoulder out to the first finger
    LE = [root + ex * a + ez * (-0.03) for a in np.linspace(0.0, SPAN + 0.02, 5)]
    k.add("leading", tube(LE, [0.03, 0.026, 0.022, 0.018, 0.014], seg=14), BROWN, "wing{s}", fuse=False, mirror=True)
    # a fold of the shoulder fused into the body, so the wing grows out of it
    k.add("shoulder", ell(root + ex * 0.04, (0.055, 0.065, 0.055)), BROWN, "wing{s}", mirror=True)

    # A fan of brown tail feathers, tilted toward the right so its faces show from behind
    for i, a in enumerate((-0.5, -0.25, 0, 0.25, 0.5)):
        b = np.array([a * 0.05, 0.24, 0.32])
        d = unit(a * 0.7, 1, -0.25)
        sp.feather(k, f"tail{i}", b, b + d * (0.26 - 0.05 * abs(a)), 0.07, 0.014, BROWN if i % 2 == 0 else DARK, "tail",
                   up=unit(0.55, 0, 0.85))

    # Short pink legs tucked under the trunk, small feet with white claws: a bird in flight
    for side in ((1, "R"), (-1, "L")):
        sp.foot(k, side, (0.065, 0.02, 0.24), (0.07, 0.04, 0.13), 0.04, sp.LEG, sp.CLAW, r=0.017)

    # Spearow's eyes, smaller and narrower under a lower brow
    look = sp.bird_eye(srgb(sp.WHITE), srgb(sp.PUPIL))
    de = unit(0.6, -0.72, 0.25)
    E = H + de / np.linalg.norm(de / HR)
    def eyes(state):
        sp.lay_eyes(k, state, E, de, (0.033, 0.031), look, lambda u: 0.15 + 0.45 * u, lambda u: -0.1 + 0.55 * u,
                    srgb(BROWN), srgb(sp.INK))

    k.build(voxel=0.007)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    fearow(*args())
