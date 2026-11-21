# Sewaddle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sewaddle.py -- <the export's Sewaddle.glb> <out.glb>
# Written to be grown from (swadloon.py and leavanny.py take its colours and its way of making eyes).
#
# The design, from the references: a short green caterpillar of three round segments trailing back low along the
# ground, orange round feet in pairs under them and one on the tail's end; a big round golden-yellow head held up at
# the front, a knot of two yellow-orange balls on its crown; a hooked orange beak over the mouth with two small white
# fangs under it and a pale grey bib over the chin and lower cheeks; small round eyes set far apart (a black rim, a
# white ring, a black pupil); a broad green leaf worn as a hood, wrapping the back and sides of the head from the
# neck up over the crown, flaring out beside the cheeks, its edge bitten and its tip lifted at the top.
# Skeleton: body (0, 0.15, 0.045), seg1 (0, 0.09, 0.05), seg2 (0, 0.035, 0.065), seg3 (0, 0, 0.1),
# head (0, -0.01, 0.13).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, hull, srgb, inside, smoothstep, unit, eye_look, args, outward

GREEN, LEAF, SEAM = "#7CC246", "#6DB83E", "#5A9A36"
YELLOW, KNOT, ORANGE, BIB = "#F5CE62", "#ECAE48", "#EF8A38", "#D9D7D1"
INK, WHITE = "#1A1A1C", "#FAFAF6"

HC = np.array([0.0, -0.04, 0.185])            # the head's middle
HR = np.array([0.096, 0.09, 0.093])           # its radii (a tenth over the reference's)

# The segments: on their bones' joints, low at the back, rising to the neck under the head
CHAIN = [("body", (0, 0.152, 0.046), 0.046), ("seg1", (0, 0.092, 0.054), 0.054),
         ("seg2", (0, 0.036, 0.066), 0.06), ("seg3", (0, -0.006, 0.098), 0.06)]


def on_head(d, c=HC, r=HR):
    """A point of the head's ellipsoid in a direction, and the outward normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def add_body(k):
    for bone, at, r in CHAIN:
        k.add(f"seg_{bone}", ell(at, (r * 1.04, r * 0.95, r)), GREEN, bone)
    k.add("tailend", ell((0, 0.196, 0.04), (0.034, 0.03, 0.034)), GREEN, "body")
    # seams between the segments, dark green rings across the back
    cuts = [np.array(a) * 0.5 + np.array(b) * 0.5 for (_, a, _), (_, b, _) in zip(CHAIN[:-1], CHAIN[1:])]
    cuts.append(np.array([0, 0.176, 0.044]))
    def seams(P):
        m = np.zeros(len(P))
        for c in cuts:
            m = np.maximum(m, inside(np.abs(P[:, 1] - c[1]) - 0.0032, 0.0022))
        return m * (P[:, 2] > 0.03) * (P[:, 1] > -0.01)
    k.paint(seams, SEAM, on=["seg", "tailend"])
    # round orange feet in pairs under the segments, and one on the tail's end
    for bone, at, r in CHAIN[:3]:
        at = np.array(at)
        k.add(f"foot_{bone}", ell(at + np.array([0.62 * r, -0.1 * r, -0.62 * r]), (0.021, 0.021, 0.02)), ORANGE,
              bone, mirror=True)
    k.add("foot_neck", ell((0.05, -0.04, 0.05), (0.021, 0.021, 0.02)), ORANGE, "seg3", mirror=True)
    k.add("foot_tail", ell((0, 0.226, 0.03), (0.02, 0.02, 0.02)), ORANGE, "body")


def add_head(k):
    k.add("head", ell(HC, HR), YELLOW, "head")
    # the knot on the crown: two balls side by side, a little forward of the top
    for i, (dx, r) in enumerate(((-0.017, 0.026), (0.019, 0.023))):
        p, n = on_head((dx * 4, -0.25, 1.0))
        k.add(f"knot{i}", ell(p + n * 0.012 + np.array([dx, 0, 0]), (r, r * 0.95, r)), KNOT, "head")
    # the pale bib over the chin and the lower cheeks
    def bib(P):
        d = (P - HC) / HR
        return inside(d[:, 2] + 0.18 + 0.25 * d[:, 0] ** 2, 0.004) * inside(d[:, 1] + 0.05, 0.004) \
            * inside(np.linalg.norm(d, axis=1) - 1.25, 0.004)
    k.paint(bib, BIB, on=["head"])


def add_beak(k):
    # a hooked orange beak: broad at the root on the face, out and curling down to a blunt point
    base = HC + np.array([0, -HR[1] * 0.93, -0.012])
    pts = [base + (0, 0.012, 0.01), base + (0, -0.014, 0.008), base + (0, -0.03, -0.006), base + (0, -0.03, -0.026),
           base + (0, -0.02, -0.036)]
    from sculptkit import blade
    k.add("beak", outward(blade(pts, [0.036, 0.034, 0.026, 0.016, 0.009], (0, 1, 0), 0.7)), ORANGE, "head",
          fuse=False)
    # two small white fangs under the beak's root
    for s in (1, -1):
        c = base + np.array([0.02 * s, 0.002, -0.026])
        k.add(f"fang{'R' if s > 0 else 'L'}", hull([c + (-0.008, -0.004, 0.006), c + (0.008, -0.004, 0.006),
                                                     c + (0, 0.006, 0.006), c + (0.001 * s, -0.002, -0.018)]),
              WHITE, "head", fuse=False)


def hood(k, colour=LEAF):
    """The leaf worn as a hood: a curved sheet round the back of the head, over the crown and out beside the cheeks."""
    X = np.array([1.0, 0, 0])
    A0, A1, TOP = math.radians(-50), math.radians(120), math.radians(80)
    def width(a):
        # the half-angle across the leaf at each place along it: broad round the head, narrowing to its tip on top
        w = np.interp(a, [A0, math.radians(-25), math.radians(40), math.radians(78), A1],
                      [0.9, 1.4, 1.48, 0.7, 0.03])
        # bites out of the edge
        for at, depth, half in ((math.radians(62), 0.22, 0.11), (math.radians(8), 0.16, 0.08)):
            w = w - depth * np.clip(1 - ((a - at) / half) ** 2, 0, 1)
        return w
    def at(u, v):
        a = A0 + (A1 - A0) * (0.01 + 0.98 * u)
        b = (2 * v - 1) * width(a)
        # past the crown the leaf stops wrapping and rises straight up, leaning a little to the model's left
        e = np.minimum(a, TOP)
        lean = -0.3 * smoothstep(math.radians(60), A1, a)
        m = np.stack([lean, np.cos(e), np.sin(e)], -1)
        m /= np.linalg.norm(m, axis=-1, keepdims=True)
        d = np.cos(b)[..., None] * m + np.sin(b)[..., None] * X
        # out from the head: flaring beside the cheeks, the tip lifted
        flare = 1.1 + 0.32 * smoothstep(0.8, 1.48, np.abs(b)) * smoothstep(math.radians(-25), math.radians(15), a) \
            * (1 - smoothstep(math.radians(60), math.radians(100), a)) \
            + 0.75 * smoothstep(TOP - 0.15, A1, a)
        return HC + d * HR * flare[..., None]
    k.add("hood", outward(sheet(at, 64, 24, 0.012)), colour, "head", fuse=False, surface="leaf")
    # its midrib, a darker line up the middle of the outside
    rib = [HC + np.array([0, math.cos(a), math.sin(a)]) * HR * 1.125 for a in np.radians([-40, 0, 40, 70])]
    k.add("rib", tube(rib, [0.005, 0.006, 0.006, 0.004]), SEAM, "head", fuse=False, surface="leaf")


EYE_D = (0.66, -0.71, 0.2)
EYE_HALF = 0.023


def eye_open(u, v, black=srgb(INK), white=srgb(WHITE)):
    r2 = u * u + v * v
    c = np.tile(black, (len(u), 1))
    c[(r2 < 0.62 ** 2) & (r2 > 0.36 ** 2)] = white
    return c


def eyes(k, state):
    look = eye_look(state, eye_open, srgb(YELLOW), srgb(INK), line=0.17)
    p, n = on_head(EYE_D)
    k.decal(p, n, (EYE_HALF, EYE_HALF), look, eps=0.004, rings=20, segs=56, mirror=True)


def sewaddle(src, out):
    k = Kit(src, coat="fur")
    add_body(k)
    add_head(k)
    add_beak(k)
    hood(k)
    k.build(voxel=0.005, smooth=3)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    sewaddle(*args())
