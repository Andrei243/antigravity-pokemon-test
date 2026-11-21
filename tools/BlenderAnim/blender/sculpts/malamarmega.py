# Mega Malamar sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/malamarmega.py -- <the export's Malamar-Mega.glb> <out.glb>
#
# From the references (10297): Malamar drawn out into a floating spear. The largest masses, in order: two great open
# rings of light beside it (green on its left, pink on its right, each some 0.4 of the height across); a long body
# tapering from broad shoulders down to a thin stalk and a white diamond spearhead, its front a rainbow (pink, yellow,
# green, cyan, blue down its length) spotted pale yellow in two columns, its sides pale lavender-grey; two broad
# manta flaps rising up and out from the neck, lavender on top and magenta underneath; a crown puffed into a ball of
# fat curled navy tentacles striped cyan; a small face tucked under the crown's front with Malamar's narrow yellow eyes
# and magenta beak; two thin navy arms reaching out to the rings and ending in pale fins.
#
# The game's sculptor names the rings, the rainbow and the point to float on; the pictures give the flaps, the
# crown's ball, the spearhead's diamond and the thin arms, which it does not have.
#
# Grown from malamar.py (sibling): its eye (shape, open look, lid) and beak.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, hull, polygon, sibling, outward, \
    slab, lumpy, tri

mal = sibling("malamar")

NAVY, CYAN, VIOLET, LAVENDER, GREY, MAGENTA = "#3C4488", "#6EE2EE", "#5C5AA0", "#C6C6E6", "#A4A6CE", "#D43C7C"
BACK, WHITE, SPOT = "#6E719E", "#E6E6F2", "#FFF4B4"
RAINBOW = [(0.645, "#F066A6"), (0.565, "#F4E070"), (0.475, "#94E27E"), (0.39, "#6CD6EA"), (0.3, "#6E86E2")]
PINK_RING, GREEN_RING = "#F07ACC", "#76E482"
Z = np.array([0, 0, 1.0])

MANTLE_C, MANTLE_R = np.array([0.0, 0.0, 0.47]), np.array([0.082, 0.058, 0.235])
HEAD_C, HEAD_R = np.array([0.0, -0.034, 0.72]), np.array([0.056, 0.05, 0.052])
CROWN_C, CROWN_R = np.array([0.0, 0.012, 0.8]), np.array([0.11, 0.092, 0.08])


def narrowing(t):
    """The body's width at a height t of its unit sphere: broad at the shoulders, drawn to a stalk below."""
    return 0.22 + 0.78 * np.clip((t + 1) / 2, 0, 1) ** 0.85


def spindle(V):
    V = V.copy()
    V[:, :2] *= narrowing(V[:, 2])[:, None]
    # the sphere's own rounding of the top is kept a little flatter: square shoulders under the collar
    V[:, :2] *= (1 + 0.25 * np.clip(V[:, 2], 0, 1) ** 2)[:, None]
    return V


def half_width(z):
    t = np.clip((z - MANTLE_C[2]) / MANTLE_R[2], -1, 1)
    return MANTLE_R[0] * narrowing(t) * np.sqrt(np.clip(1 - t * t, 0.02, 1)) * (1 + 0.25 * np.clip(t, 0, 1) ** 2)


def body(k):
    k.add("mantle", ell(MANTLE_C, MANTLE_R, deform=spindle), GREY, "body")
    k.add("stalk", tube([(0, 0.004, 0.33), (0, 0.004, 0.26), (0, 0.0, 0.2)], [0.024, 0.019, 0.016]), GREY, "body")
    # the back darker toward the stalk
    k.paint(lambda P: inside(-P[:, 1] + 0.01, 0.02) * inside(P[:, 2] - 0.55, 0.06), BACK, on=["mantle", "stalk"])
    # the rainbow down the front: a band of the front's middle, each colour softly into the next
    front = lambda P: inside(P[:, 1] + 0.012, 0.006) * inside(np.abs(P[:, 0]) - 0.62 * half_width(P[:, 2]), 0.004)
    for i, (z, col) in enumerate(RAINBOW):
        top = 9 if i == 0 else (RAINBOW[i - 1][0] + z) / 2
        bot = -9 if i == len(RAINBOW) - 1 else (RAINBOW[i + 1][0] + z) / 2
        k.paint(lambda P, top=top, bot=bot: front(P) * inside(P[:, 2] - top, 0.025) * inside(bot - P[:, 2], 0.025),
                col, on=["mantle", "stalk"])
    # the spearhead: a white diamond, flat across the front so both cameras see its face
    k.add("spear", hull([(0, 0, 0.105), (0.052, 0.0, 0.2), (-0.052, 0.0, 0.2), (0, 0.0, 0.265),
                         (0, -0.013, 0.2), (0, 0.013, 0.2), (0.02, 0, 0.245), (-0.02, 0, 0.245)]), WHITE, "body",
          fuse=False, surface="shell")


SPOTS = [(0.026, 0.6, 0.009, 0.014), (0.026, 0.53, 0.009, 0.014), (0.024, 0.46, 0.008, 0.013),
         (0.021, 0.395, 0.007, 0.012), (0.016, 0.335, 0.006, 0.011)]


def spots(k):
    for x, z, a, b in SPOTS:
        k.decal((x, -0.05, z), (0, -1, 0), (a, b), SPOT, eps=0.003, rings=10, segs=40, mirror=True)
    # (the base's yellow crescent under the beak is left out: cast round the narrow neck it came out as shards)


# ---- the collar: two manta flaps rising up and out -------------------------------------------------------------
FLAP = [(0.0, 0.0), (0.08, 0.06), (0.17, 0.13), (0.27, 0.215), (0.285, 0.18), (0.23, 0.085), (0.16, 0.01),
        (0.08, -0.045), (0.0, -0.065)]


def flaps(k):
    for s, side in ((1, "R"), (-1, "L")):
        ex = unit(s, 0.18, 0)                  # out to the side, swept a little back
        ez = unit(0, 0.25, 1)                  # up, leaning back
        o = np.array([0.04 * s, 0.01, 0.665])
        k.add(f"flap{side}", slab(FLAP, o, ex, ez, 0.022), LAVENDER, f"arm{side}", fuse=False, surface="scales")
        under = [(a, b - 0.022) for a, b in FLAP]
        k.add(f"flapu{side}", slab(under, o + np.array([0, 0.012, 0]), ex, ez, 0.02), MAGENTA, f"arm{side}",
              fuse=False, surface="scales")
    k.add("neck", ell((0, 0.005, 0.67), (0.07, 0.055, 0.04)), NAVY, "body")


# ---- the head and its crown ------------------------------------------------------------------------------------
def head(k):
    k.add("head", ell(HEAD_C, HEAD_R), VIOLET, "head")
    k.add("crown", ell(CROWN_C, CROWN_R * 0.85), NAVY, "head")
    # fat tentacles rolled up and out from the crown's middle, each a lobe of the ball
    for i, a in enumerate(LOBES):
        r = math.radians(a)
        d = np.array([math.sin(r), -math.cos(r) * 0.85, 0.0])
        root = CROWN_C + d * 0.02 + np.array([0, 0, -0.01])
        pts = [root, root + d * 0.05 + Z * 0.065, root + d * 0.095 + Z * 0.035, root + d * 0.105 - Z * 0.015]
        k.add(f"crownlobe{i}", tube(pts, [0.04, 0.04, 0.034, 0.026]), NAVY, "head")
    # the curls hanging at the crown's sides, rolled under like the picture's lowest tentacles
    for s in (1, -1):
        for j, (fx, fz, back) in enumerate(((0.09, 0.79, 0.01), (0.07, 0.76, 0.06))):
            a = np.array([fx * s, back, fz])
            pts = [a, a + np.array([0.04 * s, -0.01, -0.01]), a + np.array([0.055 * s, -0.012, -0.05]),
                   a + np.array([0.035 * s, -0.01, -0.075])]
            k.add(f"curl{j}{'R' if s > 0 else 'L'}", tube(pts, [0.036, 0.032, 0.026, 0.02]), NAVY, "head")
    # cyan down the middle of each lobe: bands by angle round the crown's upright axis, above the face
    k.paint(stripe_mask, CYAN, on=["crown", "curl"])


LOBES = (-120, -82, -44, -14, 16, 46, 84, 122, 160, -160)


def stripe_mask(P):
    """A broad cyan stripe down the middle of each lobe, by angle round the crown's upright axis."""
    d = P - CROWN_C
    az = np.degrees(np.arctan2(d[:, 0], -d[:, 1] / 0.85))
    near = np.min([np.abs((az - a + 180) % 360 - 180) for a in LOBES], axis=0)
    return inside(near - 5.0, 1.2) * inside(0.765 - P[:, 2], 0.01)


def beak(k):
    f = HEAD_C[1] - HEAD_R[1]
    zc = HEAD_C[2] - 0.012
    pts = [(0, f + 0.012, zc + 0.02), (0.017, f + 0.004, zc), (-0.017, f + 0.004, zc), (0, f - 0.016, zc - 0.004),
           (0, f - 0.006, zc - 0.052), (0, f + 0.014, zc - 0.025)]
    k.add("beak", hull(pts), MAGENTA, "head", fuse=False)


def eyes(k):
    def lay(state):
        for s in (1, -1):
            n = unit(0.42 * s, -0.9, 0.06)
            c = HEAD_C + np.array([0.035 * s, -HEAD_R[1] * 0.86, 0.012])
            look = eye_look(state, mal.open_eye, srgb(VIOLET), srgb(mal.INK), line=0.17)
            col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
            shape = mal.EYE_SHAPE if s > 0 else [(-u, v) for u, v in mal.EYE_SHAPE]
            k.decal(c, n, (0.03, 0.02), col, outline=polygon(shape), eps=0.0035, rings=20, segs=56)
    return lay


# ---- the arms and the rings ------------------------------------------------------------------------------------
RING_C, RING_R = np.array([0.28, -0.005, 0.605]), 0.118


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        pts = [np.array(p) * m for p in ((0.05, 0.0, 0.63), (0.13, -0.01, 0.605), (0.2, -0.02, 0.555),
                                         (0.26, -0.022, 0.51), (0.315, -0.022, 0.49))]
        k.add(f"arm{side}", tube(pts, [0.02, 0.016, 0.014, 0.014, 0.013]), NAVY, f"arm{side}")
        e = pts[-1]
        fin = [e + np.array(p) * m for p in ((-0.01, 0, 0.012), (0.0, 0, -0.012), (0.075, 0, -0.045),
                                               (0.05, 0, 0.005), (0.02, -0.01, 0), (0.02, 0.01, 0))]
        k.add(f"fin{side}", hull(fin), WHITE, f"arm{side}", fuse=False, surface="shell")


def rings(k):
    for s, side, col in ((1, "R", PINK_RING), (-1, "L", GREEN_RING)):
        c = RING_C * np.array([s, 1, 1.0])
        e1 = unit(s, -0.3, 0)                 # the ring's plane turned some 17 degrees toward the front
        open_at = math.radians(-50)           # the gap faces down and out
        th = open_at + np.linspace(math.radians(32), math.radians(328), 40)
        pts = [c + RING_R * (math.cos(t) * e1 + math.sin(t) * Z) for t in th]
        v = np.linspace(0, 1, len(pts))
        rad = 0.008 + 0.018 * np.sin(math.pi * v) ** 0.6
        k.add(f"ring{side}", outward(tube(pts, rad, seg=14, per=3)), col, f"arm{side}", fuse=False, glow=col,
              surface="plastic")


def malamar_mega(src, out):
    k = Kit(src, coat="scales")
    body(k)
    flaps(k)
    head(k)
    arms(k)
    rings(k)
    beak(k)
    spots(k)
    k.build(voxel=0.0072, smooth=3, band=0.006)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    malamar_mega(src, out)
