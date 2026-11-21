# Yveltal sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/yveltal.py -- <the export's Yveltal.glb> <out.glb>
#
# From the references: a great bird of destruction hovering in the shape of a Y. Its two wings, raised in a V, are
# broad red blades widening to their ends, cracked with black lines, each ending in a hand of five long black claws;
# its tail hangs below as the Y's stem, a red blade of its own ending in five black claws. A slim red body, black down
# the back where curved black spines hang like a cape; a grey ruff of smoke rising behind the neck. A small black head
# with a red lower face, a short dark hooked beak, a crest of black spikes swept back, a cyan eye under a scowl. Short
# red legs tucked under the body with grey talons.
# Bones: body (0, 0, 0.536); wings (+-0.06, 0.02, 0.576); head (0, -0.12, 0.796); legs (+-0.04, 0, 0.456).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, tri, unit, lumpy, args, sibling

r = sibling("raikou")            # the point on an ellipsoid, the eyes cut by a brow
a = sibling("articuno")          # the wing's axes
sp = sibling("spearow")          # the solid kite of a claw

RED = "#D21E32"
BLACK = "#25222B"
CRACK = "#1A171E"
GREY = "#9C9AA4"
BEAK = "#3A3640"
TALON = "#B8B6BE"
EYE, IRIS, PUPIL = "#E6F8FF", "#30BEF2", "#0C1622"

H = np.array([0.0, -0.155, 0.81])
HR = np.array([0.058, 0.078, 0.055])
EYE_DIR = unit(0.6, -0.74, 0.2)
EYE_R = 0.018


def plane(root, ex, ez):
    root, ex, ez = np.array(root, float), np.array(ex, float), np.array(ez, float)
    return lambda p, q: root + ex * p + ez * q


def cracks(k, name, lines, origin, ex, ez, thick, bone, w=0.026):
    """Black cracks across a red blade: each segment of each line a strip thicker than the blade, so it shows on
    both faces."""
    n = 0
    for line in lines:
        for p, q in zip(line, line[1:]):
            p, q = np.array(p, float), np.array(q, float)
            d = (q - p) / np.linalg.norm(q - p)
            nn = np.array((-d[1], d[0]))
            p0, q0 = p - d * w * 0.4, q + d * w * 0.4
            out_ = [p0 + nn * w / 2, q0 + nn * w / 2, q0 - nn * w / 2, p0 - nn * w / 2]
            k.add(f"{name}{n}", slab(out_, origin, ex, ez, thick), CRACK, bone, fuse=False, mirror=(bone != "body"))
            n += 1


def claws(k, name, at, nrm, bases, dirs, lengths, curl, bone, mirror, w=0.05, t=0.026):
    """A hand of black claws: solid kites from points of a blade's edge, each curling a little toward curl."""
    for j, (b, d, L) in enumerate(zip(bases, dirs, lengths)):
        base = at(*b)
        d = unit(*d)
        tip = base + d * L + np.array(curl) * L * 0.25
        k.add(f"{name}{j}", sp.spike(base - d * 0.02, tip, w, t, up=nrm), BLACK, bone, fuse=False, mirror=mirror,
              surface="shell")


# --- the wings: raised in a V, broad red blades widening to a hand of five black claws -------------------------------
ELEV, SWEEP = 40, 14
ROOT = np.array([0.075, 0.02, 0.625])
WING = [(0.0, -0.035), (0.16, -0.04), (0.3, -0.05), (0.375, -0.025), (0.39, 0.08), (0.36, 0.17), (0.27, 0.15),
        (0.13, 0.08), (0.0, 0.045)]
WING_CRACKS = [[(0.04, 0.0), (0.1, 0.03), (0.16, 0.0), (0.22, 0.04), (0.29, 0.015), (0.35, 0.06)],
               [(0.2, 0.075), (0.26, 0.1), (0.33, 0.13)]]
FINGERS = [((0.365, -0.03), -4, 0.19), ((0.38, 0.015), 18, 0.2), ((0.385, 0.065), 40, 0.19), ((0.37, 0.115), 62, 0.17),
           ((0.34, 0.16), 84, 0.15)]


def wings(k):
    ex, ez = a.wing_axes(ELEV, SWEEP, (0.1, 0.35, -1.0))
    n = np.cross(ex, ez)
    k.add("shoulder", ell(ROOT - ex * 0.01, (0.055, 0.055, 0.055)), RED, "wing{s}", mirror=True)
    k.add("arm", tube([ROOT - ex * 0.04, ROOT + ex * 0.05, ROOT + ex * 0.1], [0.05, 0.044, 0.04]), RED, "wing{s}",
          mirror=True)
    k.add("blade", slab(WING, ROOT, ex, ez, 0.038), RED, "wing{s}", fuse=False, mirror=True, surface="skin")
    cracks(k, "wcrack", WING_CRACKS, ROOT, ex, ez, 0.05, "wing{s}")
    at = plane(ROOT, ex, ez)
    dirs = [ex * math.cos(math.radians(g)) + ez * math.sin(math.radians(g)) for _, g, _ in FINGERS]
    claws(k, "claw", at, n, [b for b, _, _ in FINGERS], dirs, [L for _, _, L in FINGERS], ez, "wing{s}", True,
          w=0.042, t=0.026)


# --- the tail: the Y's stem, a red blade hanging below, five black claws at its end ----------------------------------
TAIL_O = np.array([0.0, 0.06, 0.37])
TAIL_EZ = unit(0, 0.3, -1.0)
TAIL = [(-0.035, 0.0), (0.035, 0.0), (0.05, 0.1), (0.09, 0.18), (0.08, 0.23), (-0.08, 0.23), (-0.09, 0.18),
        (-0.05, 0.1)]


def tail(k):
    k.add("tail", tube([(0, 0.02, 0.45), (0, 0.04, 0.4), (0, 0.055, 0.37)], [0.055, 0.045, 0.036]), RED, "body")
    ex = np.array((1.0, 0, 0))
    n = np.cross(ex, TAIL_EZ)
    k.add("tblade", slab(TAIL, TAIL_O, ex, TAIL_EZ, 0.036), RED, "body", fuse=False, surface="skin")
    cracks(k, "tcrack", [[(0.0, 0.02), (0.025, 0.07), (-0.01, 0.12), (0.025, 0.17), (-0.005, 0.21)]], TAIL_O, ex,
           TAIL_EZ, 0.048, "body")
    at = plane(TAIL_O, ex, TAIL_EZ)
    bases, dirs, lens = [], [], []
    for j, g in enumerate((-48, -24, 0, 24, 48)):
        t = math.radians(g)
        bases.append((0.075 * math.sin(t) * 1.3, 0.215))
        dirs.append(ex * math.sin(t) + TAIL_EZ * math.cos(t))
        lens.append(0.13 - 0.01 * abs(j - 2))
    claws(k, "tclaw", at, n, bases, dirs, lens, np.array((0, 0.4, 0)), "body", False, w=0.04, t=0.024)


def body(k):
    k.add("torso", ell((0, 0.015, 0.53), (0.075, 0.08, 0.13)), RED, "body")
    k.add("chest", ell((0, -0.03, 0.63), (0.07, 0.068, 0.075)), RED, "body")
    k.add("hips", ell((0, 0.02, 0.43), (0.058, 0.062, 0.07)), RED, "body")
    k.add("neck", tube([(0, -0.03, 0.66), (0, -0.085, 0.74), (0, -0.125, 0.785)], [0.05, 0.043, 0.038]), RED, "head")
    # black down the back
    k.paint(lambda P: inside(0.03 - P[:, 1], 0.004) * (P[:, 2] > 0.33) * (P[:, 2] < 0.7), BLACK,
            on=["torso", "chest", "hips", "tail"])
    # black cracks down the red front
    def crack(P):
        zig = 0.018 * tri(P[:, 2] / 0.07)
        return inside(np.abs(P[:, 0] - 0.012 - zig) - 0.007, 0.002) * (P[:, 1] < -0.03) * (P[:, 2] > 0.42) \
            * (P[:, 2] < 0.66)
    k.paint(crack, CRACK, on=["torso", "chest", "hips"])
    # the ruff of grey smoke rising behind the neck
    for i, (c, rr) in enumerate((((0, 0.06, 0.7), (0.07, 0.06, 0.08)), ((0, 0.085, 0.8), (0.055, 0.05, 0.075)),
                                 ((0.025, 0.09, 0.88), (0.042, 0.04, 0.045)))):
        k.add(f"ruff{i}", ell(c, rr, deform=lumpy(4, 3, 0.08)), GREY, "head" if i else "body")
    # billows to the sides, so the smoke reads as a cloud and not a post
    k.add("puff", ell((0.055, 0.075, 0.76), (0.042, 0.042, 0.048), deform=lumpy(3, 2, 0.1)), GREY, "head", mirror=True)
    k.add("puffhi", ell((-0.03, 0.08, 0.84), (0.035, 0.035, 0.04)), GREY, "head")
    # the black spines hanging down the back like a cape
    for j, (x, y, z, d, L) in enumerate(((0.0, 0.07, 0.62, (0, 0.75, -0.55), 0.17), (0.0, 0.085, 0.52, (0, 0.8, -0.6), 0.16),
                                         (0.0, 0.075, 0.43, (0, 0.7, -0.7), 0.13), (0.05, 0.06, 0.58, (0.4, 0.7, -0.6), 0.14),
                                         (-0.05, 0.06, 0.58, (-0.4, 0.7, -0.6), 0.14))):
        base = np.array((x, y, z))
        k.add(f"spine{j}", sp.spike(base, base + unit(*d) * L, 0.06, 0.026, up=(1, 0, 0)), BLACK, "body", fuse=False,
              surface="shell")


def legs(k):
    k.add("thigh", ell((0.045, -0.03, 0.43), (0.036, 0.042, 0.045)), RED, "leg{s}", mirror=True)
    k.add("shin", tube([(0.05, -0.045, 0.41), (0.056, -0.07, 0.36)], [0.026, 0.02]), RED, "leg{s}", mirror=True)
    for j, dx in enumerate((-0.016, 0.0, 0.016)):
        base = np.array((0.056 + dx, -0.075, 0.36))
        k.add(f"talon{j}", sp.spike(base, base + np.array((dx * 0.8, -0.04, -0.045)), 0.02, 0.014, up=(0, 0, 1)), TALON,
              "leg{s}", fuse=False, mirror=True, surface="shell")


def head(k):
    k.add("head", ell(H, HR), BLACK, "head")
    k.add("cheek", ell(H + (0, -0.03, -0.022), (0.044, 0.05, 0.032)), RED, "head")
    # the red lower face, its edge crisp under the eyes
    k.paint(lambda P: inside(P[:, 2] - (H[2] - 0.018 + 0.2 * (P[:, 1] - H[1])), 0.003) * (P[:, 1] < H[1] + 0.025)
            * (np.linalg.norm((P - H) / (0.08, 0.11, 0.08), axis=1) < 1), RED)
    k.add("beak", tube([H + (0, -0.055, -0.012), H + (0, -0.1, -0.022), H + (0, -0.118, -0.048)], [0.022, 0.015, 0.009],
                       seg=20), BEAK, "head", fuse=False, surface="shell")
    # the crest of black spikes swept back
    for j, (b, t, w) in enumerate((((0, 0.0, 0.04), (0, 0.14, 0.1), 0.05), ((0.03, 0.02, 0.03), (0.085, 0.14, 0.06), 0.045),
                                   ((-0.03, 0.02, 0.03), (-0.085, 0.14, 0.06), 0.045),
                                   ((0.04, 0.03, 0.0), (0.105, 0.11, -0.015), 0.04),
                                   ((-0.04, 0.03, 0.0), (-0.105, 0.11, -0.015), 0.04))):
        k.add(f"crest{j}", sp.spike(H + b, H + t, w, 0.022, up=(0, 0, 1)), BLACK, "head", fuse=False, surface="shell")


def eye_top(u):
    return 0.22 + 0.5 * u


def eyes(k, state):
    p, n = r.on_ell(H, HR, EYE_DIR)
    r.lay_eyes(k, state, p, n, EYE_R, r.eye_open(EYE, IRIS, PUPIL, iris_r=(0.72, 0.86)), BLACK, "#C4C2CC", eye_top)


def yveltal(src, out):
    k = Kit(src, coat="fur")
    body(k)
    head(k)
    legs(k)
    wings(k)
    tail(k)
    k.build(voxel=0.0065, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    yveltal(*args())
