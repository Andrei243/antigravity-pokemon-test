# Avalugg sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/avalugg.py -- <the export's Avalugg.glb> <out.glb>
# Grown from bergmite.py: the same ice and lilac, the same crystals (its crystal(), loose flat-faced hulls), the same
# yellow-orange eyes rimmed lilac with a dark pupil, now small and hard under a brow of ice.
#
# The design, from the references: a great flat top of ice like a table, laid as four plates across its width that
# step up from the front to the back, each step's face and the crack along it purple, their sides jagged; the back
# plate the smallest, set to the right. Under it a chunky mass of deeper blue ice, and four thick legs of crystal,
# each with a purple band round its lower part and a foot that splays into pointed crystal toes. The head is small,
# low under the top's front edge: a block of deep ice with a flat plate of pale ice over it like a visor, small
# angry almond eyes under it, a white crystal tusk at each side of the jaw and little icicles under the chin.
# Where the task's text differed: the top is not one slab with cracks but plates in steps; the face is not a mark
# but a head of its own. The skeleton: body (0, 0, 0.28), head (0, -0.2, 0.22), the legs' roots at z 0.22
# (x 0.14, y -0.12 and 0.14); the export is 0.40 tall. The top is kept within the game's board: width 0.44 against
# a length of 0.68 (0.853 w + 0.522 l = 0.73 inside a square of 0.75).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, boxy, hull, slab, polygon, args, sibling

bm = sibling("bergmite")
crystal, jitter = bm.crystal, bm.jitter

# --- colours: Bergmite's, deepened ------------------------------------------------------------------------------------
TOP, TOP2, SIDE = "#CDE9F8", "#C2E2F6", bm.ICE
UNDER, UNDER2, LEG = "#93B6E6", "#A6C4EC", "#9AB2E4"
CRACK, BAND = "#8466C2", "#7E62BC"
TOE, TUSK = "#D8E6F8", "#F2F7FC"
LILAC, IRIS, PUPIL, INK = bm.LILAC, "#F4A626", bm.PUPIL, bm.INK

# --- the frame --------------------------------------------------------------------------------------------------------
HALF_W = 0.22                     # the top's half width
SLAB_BOTTOM = 0.3
# the plates, front to back: (front edge's y at the middle, back edge's y, top, x from, x to)
PLATES = [(-0.322, -0.12, 0.346, -HALF_W, HALF_W),
          (-0.142, 0.03, 0.37, -HALF_W, HALF_W),
          (0.008, 0.18, 0.394, -HALF_W + 0.01, HALF_W),
          (0.158, 0.318, 0.416, -0.09, HALF_W - 0.01)]
SLANT = 0.028                     # the steps run a little diagonally: the front edge's y at +X is this further back
HEAD_C, HEAD_R = np.array([0.0, -0.285, 0.205]), (0.086, 0.076, 0.062)
EYE_DIR = unit(0.56, -0.8, 0.16)
EYE_HALF = (0.028, 0.019)
VOXEL = 0.0038


def edge(xs, y, seed, amp=0.008):
    """A jagged edge: points along x at about y, each nudged forward or back."""
    rs = np.random.RandomState(seed)
    return [(x, y + SLANT * x / HALF_W + (rs.uniform(-amp, amp) if 0 < i < len(xs) - 1 else 0))
            for i, x in enumerate(xs)]


def plate_outline(i):
    y0, y1, top, x0, x1 = PLATES[i]
    n = 7
    xs = list(np.linspace(x0, x1, n))
    front = edge(xs, y0, 10 + i)
    back = edge(xs[::-1], y1, 20 + i, 0.012)
    rs = np.random.RandomState(30 + i)
    def side(x, ya, yb):
        ys = np.linspace(ya, yb, 4)[1:-1]
        return [(x + rs.uniform(-0.012, 0.006) * np.sign(x), y) for y in ys]
    # the corners cut off a little, so the footprint is no full rectangle
    c = 0.022
    pts = front[:]
    pts[0] = (x0 + c * 0.6, front[0][1] + c * 0.2)
    pts[-1] = (x1 - c * 0.6, front[-1][1] + c * 0.2)
    pts += side(x1, front[-1][1], back[0][1])
    pts += back
    pts += side(x0, back[-1][1], front[0][1])
    return pts


def top(k):
    for i, (y0, y1, z, x0, x1) in enumerate(PLATES):
        out_ = plate_outline(i)
        thick = z - SLAB_BOTTOM
        k.add(f"plate{i}", slab(out_, (0, 0, z - thick / 2), (1, 0, 0), (0, 1, 0), thick),
              TOP if i % 2 == 0 else TOP2, "body", fuse=False, surface="shell")
        if i == 0:
            continue
        # the crack before each step: a purple plate whose face shows under the step and along its foot
        lower = PLATES[i - 1][2]
        front = [p for p in out_[:7]]
        strip = [(x, y - 0.022) for x, y in front] + [(x, y + 0.004) for x, y in front[::-1]]
        h = z - 0.004 - (lower - 0.004)
        k.add(f"crack{i}", slab(strip, (0, 0, lower - 0.004 + h / 2), (1, 0, 0), (0, 1, 0), h), CRACK, "body",
              fuse=False, surface="shell")


def underside(k):
    """The chunky mass of deeper ice under the top, low enough that the legs are short."""
    pts = []
    for z, sx, sy in ((0.3, 0.2, 0.29), (0.23, 0.2, 0.28), (0.14, 0.17, 0.23), (0.105, 0.11, 0.15)):
        for a in range(10):
            th = 2 * math.pi * (a + 0.5 * (z < 0.2)) / 10
            pts.append((sx * math.sin(th), 0.01 - sy * math.cos(th), z))
    k.add("under", hull(jitter(pts, 0.012, 7)), UNDER, "body", fuse=False, surface="shell")
    # a few blocks of ice standing out of it at the sides, as the pictures have
    for j, (c, d) in enumerate((((0.17, -0.02, 0.22), (1, -0.2, -0.3)), ((-0.17, 0.05, 0.21), (-1, 0.2, -0.35)),
                                ((0.15, 0.2, 0.2), (0.8, 0.5, -0.3)), ((-0.15, -0.2, 0.2), (-0.8, -0.4, -0.3)))):
        c = np.array(c); d = unit(*d)
        k.add(f"block{j}", crystal(c - d * 0.02, c + d * 0.05, [(0, 0.04), (0.7, 0.035), (1, 0.012)], n=5, turn=10 * j),
              UNDER2, "body", fuse=False, surface="shell")


def legs(k):
    for name, y, bone in (("leg", -0.12, "leg{s}"), ("hind", 0.14, "hind{s}")):
        for s, side in ((1, "R"), (-1, "L")):
            b = bone.format(s=side)
            x = 0.14 * s
            k.add(f"{name}{side}", crystal((x, y, 0.27), (x + 0.012 * s, y, 0.03),
                                           [(0, 0.078), (0.45, 0.072), (0.8, 0.064), (1.0, 0.07)], n=6, turn=15),
                  LEG, b, fuse=False, surface="shell")
            k.add(f"{name}band{side}", crystal((x + 0.01 * s, y, 0.125), (x + 0.011 * s, y, 0.098),
                                               [(0, 0.073), (1, 0.073)], n=6, turn=15),
                  BAND, b, fuse=False, surface="shell")
            # the toes: pointed crystals splaying out and down round the foot
            for t in range(5):
                a = math.radians(-100 + 50 * t) if y < 0 else math.radians(-80 + 40 * t)
                d = np.array([math.sin(a) * s, -math.cos(a), 0.0])
                root = np.array([x + 0.012 * s, y, 0.05]) + d * 0.04
                tip = root + d * 0.045 + np.array([0, 0, -0.05])
                k.add(f"{name}toe{t}{side}", crystal(root, tip, [(0, 0.022), (0.4, 0.02), (1, 0)], n=4, turn=45),
                      TOE, b, fuse=False, surface="shell")


def on_head(d):
    """Where a direction from the head's middle meets its ellipsoid."""
    d = np.array(d, float)
    t = 1 / math.sqrt(sum((d[i] / HEAD_R[i]) ** 2 for i in range(3)))
    return HEAD_C + d * t


def head(k):
    k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(2.6)), UNDER, "head", surface="shell")
    # the visor: a flat plate of pale ice over the head, jutting forward, its front edge jagged
    visor = [(-0.095, -0.21, 0.0), (0.095, -0.21, 0.0), (0.1, -0.33, 0.0), (0.056, -0.38, 0.0), (0.012, -0.364, 0.0),
             (-0.034, -0.384, 0.0), (-0.092, -0.34, 0.0)]
    out_ = [(x, y) for x, y, _ in visor]
    k.add("visor", slab(out_, (0, 0, 0.27), (1, 0, 0), (0, 1, 0), 0.028), TOP, "head", fuse=False, surface="shell")
    # the tusks: white crystals at the jaw's sides pointing down and forward
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.066 * s, -0.325, 0.186])
        k.add(f"tusk{side}", crystal(root, root + np.array([0.022 * s, -0.045, -0.07]),
                                     [(0, 0.022), (0.35, 0.02), (1, 0)], n=5, turn=20),
              TUSK, "head", fuse=False, surface="shell")
    # little icicles under the chin
    for j, x in enumerate((-0.032, 0.0, 0.032)):
        root = np.array([x, -0.315, 0.152])
        k.add(f"chin{j}", crystal(root, root + np.array([0.0, -0.01, -0.03]), [(0, 0.012), (1, 0)], n=4, turn=45),
              TOE, "head", fuse=False, surface="shell")
    # the mouth: a short dark line across the front, low
    c = on_head(unit(0, -1, -0.45))
    k.decal(c, unit(0, -1, -0.45), (0.034, 0.0045), INK, eps=0.002, rings=4, segs=40)


# --- the eyes: small hard almonds, cut by the brow on a slope down to the inner corner -------------------------------
EYE_PTS = [(-1, -0.05), (-0.7, -0.55), (-0.1, -0.85), (0.5, -0.75), (0.95, -0.25), (1.0, 0.25), (0.72, 0.72),
           (0.1, 0.55), (-0.5, 0.32), (-0.95, 0.16)]
OUTLINE = polygon(EYE_PTS)


def eye_open():
    lil, iris, pup = srgb(LILAC), srgb(IRIS), srgb(PUPIL)
    def look(u, v):
        th = np.arctan2(v, u)
        rr = np.sqrt(u * u + v * v) / np.array([OUTLINE(t) for t in th])
        c = np.tile(iris, (len(u), 1))
        c[((u - 0.05) / 0.3) ** 2 + ((v + 0.02) / 0.5) ** 2 < 1] = pup
        c[rr > 0.74] = lil
        return c
    return look


def eyes(k, state):
    lid = srgb(UNDER)
    look = eye_look(state, eye_open(), lid, srgb(INK), line=0.17)
    for s in (1, -1):
        m = np.array([s, 1.0, 1.0])
        d = EYE_DIR * m
        if s > 0:
            outline, f = OUTLINE, look
        else:
            outline = lambda th: OUTLINE(math.pi - th)
            f = lambda u, v: look(-u, v)
        k.decal(on_head(d), d, EYE_HALF, f, eps=0.0026, outline=outline, rings=20, segs=56)


def avalugg(src, out):
    k = Kit(src, coat="shell")
    top(k)
    underside(k)
    legs(k)
    head(k)
    k.build(voxel=VOXEL, band=0.004, wband=0.03)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    avalugg(*args())
