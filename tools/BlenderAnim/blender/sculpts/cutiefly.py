# Cutiefly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cutiefly.py -- <the export's Cutiefly.glb> <out.glb>
#
# From the references: a tiny bee fly whose round yellow head is nearly all of it, the largest mass by far; a white
# fluffy ruff of points behind and under the head where a body would be; a long thin brown proboscis straight out of
# the front of the face, a little down; big dark rounded-square eyes either side of its root, each with a square white
# glint high and a brown square low; two tiny pale yellow curled antennae on the crown; four thin black legs hanging
# down and back from under the ruff, each ending in a black ball; one pair of broad pale white wings raised high and
# back, each with a tan oval loop near its root.
# (The task's "small wings": the pictures' wings are broad, as long as the head is wide; kept a little shorter than
# drawn so the head stays large in the game.)
# Written to be grown from (ribombee.py imports it with sibling("cutiefly")): the eye, the wing, the leg, the spike.
# Skeleton: body (0, 0, 0.135), wings (+-0.03, 0.02, 0.175).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, lathe, srgb, unit, eye_look, args, outward

# --- the line's colours ----------------------------------------------------------------------------------------------
YELLOW = "#F4DE5E"
RUFF = "#F4F2EA"
BROWN = "#A47850"
DARK = "#28242C"
WING = "#E6E8EE"
LOOP = "#C8A27A"
EYE_DARK, EYE_BROWN, GLINT = srgb("#1E1A22"), srgb("#8A5A3A"), np.array([1.0, 1.0, 1.0])
INK = srgb("#3A2A22")

# --- Cutiefly's frame ------------------------------------------------------------------------------------------------
HC = np.array([0.0, -0.006, 0.14])          # the head, which is nearly all of it
HR = np.array([0.06, 0.062, 0.056])
RUFF_C = np.array([0.0, 0.032, 0.125])       # the ruff's middle, behind and under the head
EYE_D = (0.44, -0.88, -0.04)
EYE_HALF = (0.019, 0.022)
WING_ROOT = np.array([0.03, 0.022, 0.172])


def on_ell(c, r, d):
    """The point of an ellipsoid (centre c, radii r) along the direction d from its centre, and its normal there."""
    d = unit(*d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def rounded_square(th):
    """A decal's outline: a square with round corners."""
    c, s = abs(math.cos(th)), abs(math.sin(th))
    return 1.0 / (c ** 4 + s ** 4) ** 0.25 * 0.9


def eye_open(dark=EYE_DARK, low=EYE_BROWN, rim=None, rim_w=0.0):
    """The line's eye: dark, a square white glint high and outward, a brown square low and inward; a rim if given."""
    def look(u, v):
        c = np.tile(dark, (len(u), 1))
        c[(np.abs(u + 0.22) < 0.3) & (np.abs(v + 0.36) < 0.28)] = low
        c[(np.abs(u - 0.2) < 0.26) & (np.abs(v - 0.3) < 0.26)] = GLINT
        if rim is not None:
            c[np.maximum(np.abs(u), np.abs(v)) > 1 - rim_w] = rim
        return c
    return look


def lay_eyes(k, c, r, d, half, look, lid, state, ink=INK):
    """Both eyes as rounded squares on the head, +u outward on both."""
    p, nrm = on_ell(c, r, d)
    k.decal(p, nrm, half, eye_look(state, look, srgb(lid) if isinstance(lid, str) else lid, ink, line=0.16),
            eps=0.0015, outline=rounded_square, rings=20, segs=56, mirror=True)


def wing_outline(length, width, n=40):
    """A broad wing from its narrow root (a = 0) to a round end (a = length), widest past its middle."""
    pts = []
    for i in range(n):
        ph = 2 * math.pi * i / n
        x = length / 2 * (1 - math.cos(ph))
        y = width * math.sin(ph) * (0.3 + 0.7 * math.sin(ph / 2) ** 1.2)
        pts.append((x, y))
    return pts


def oval(a0, a1, w, n=36):
    """An oval from a0 to a1 along the wing, half width w."""
    return [((a0 + a1) / 2 + (a1 - a0) / 2 * math.cos(2 * math.pi * i / n), w * math.sin(2 * math.pi * i / n))
            for i in range(n)]


def wing_frame(out, normal):
    """The wing's two directions on +X: ex along its length, ez across, its face toward normal."""
    ex = unit(*out)
    n = np.array(normal, float); n = n - ex * (n @ ex); n /= np.linalg.norm(n)
    ez = np.cross(n, ex)
    return ex, ez


def wing(k, root, out, normal, length, width, marks, bone="wing{s}", colour=WING, thick=0.006):
    """A broad pale wing (on +X, mirrored); marks: (outline, colour) plates stacked thicker over it, each in turn."""
    ex, ez = wing_frame(out, normal)
    k.add("wing", slab(wing_outline(length, width), root, ex, ez, thick), colour, bone, fuse=False, mirror=True)
    for i, (outline, col) in enumerate(marks):
        k.add(f"wingmark{i}", slab(outline, root, ex, ez, thick + 0.0028 * (i + 1)), col, bone, fuse=False, mirror=True)


def leg(k, name, pts, r, ball=None, bone="body", colour=DARK, mirror=True):
    """A thin dark leg through pts (on +X, mirrored), ending in a ball when ball is given."""
    k.add(name, outward(tube(pts, [r] * len(pts))), colour, bone, fuse=False, mirror=mirror)
    if ball:
        p, q = np.array(pts[-1], float), np.array(pts[-2], float)
        d = (p - q) / np.linalg.norm(p - q)
        k.add(name + "ball", outward(ell(p + d * ball * 0.4, (ball,) * 3, sub=3)), colour, bone, fuse=False,
              mirror=mirror)


def spike(root, d, length, w):
    """A white point of fur: a four-sided solid from a square root of side w along d."""
    a = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a) < 1e-3:
        a = np.array([1.0, 0, 0])
    a /= np.linalg.norm(a); b = np.cross(d, a)
    root = np.array(root, float)
    pts = [root + (a * sa + b * sb) * w / 2 for sa in (-1, 1) for sb in (-1, 1)]
    pts.append(root + np.array(d) * length)
    return hull(pts)


def ruff(k, c, axis, radius, n, length, w, colour=RUFF, bone="body", seed=3, skip=None):
    """A ring of points round the axis through c, pointing out and a little back."""
    rs = np.random.RandomState(seed)
    axis = unit(*axis)
    a = np.cross(axis, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(axis, a)
    for i in range(n):
        t = 2 * math.pi * (i + 0.3 * rs.rand()) / n
        if skip and skip(t):
            continue
        rad = math.cos(t) * a + math.sin(t) * b
        d = unit(*(rad + axis * 0.45))
        L = length * (0.8 + 0.4 * rs.rand())
        k.add(f"ruff{i}", spike(c + rad * radius, d, L, w), colour, bone, fuse=False)


def cutiefly(src, out):
    k = Kit(src, coat="fur")
    # The head, nearly all of it, and the white ruff behind and under it
    k.add("head", ell(HC, HR), YELLOW, "body")
    k.add("ruffbody", ell(RUFF_C, (0.046, 0.042, 0.046)), RUFF, "body")
    ruff(k, RUFF_C + (0, 0.012, 0), (0, 1, 0), 0.04, 16, 0.026, 0.013)
    ruff(k, RUFF_C + (0, 0.03, 0.0), (0, 1, 0), 0.022, 8, 0.022, 0.012, seed=5)
    # The proboscis: brown, long and thin, straight out of the face's front a little down
    base, nrm = on_ell(HC, HR, (0, -1, -0.22))
    pd = unit(0, -1, -0.2)
    k.add("proboscis", lathe(base - pd * 0.006, pd, [(0, 0), (0.0075, 0.0), (0.0068, 0.012), (0.0042, 0.05),
                                                    (0, 0.056)]), BROWN, "body", fuse=False, surface="shell")
    # Two tiny curled antennae on the crown
    for sx in (1, -1):
        p0, _ = on_ell(HC, HR, (0.22 * sx, -0.45, 0.86))
        pts = [p0 - (0, 0, 0.004), p0 + (0.002 * sx, -0.004, 0.014), p0 + (0.007 * sx, -0.012, 0.022),
               p0 + (0.013 * sx, -0.017, 0.019), p0 + (0.014 * sx, -0.014, 0.012)]
        k.add("antenna" + ("R" if sx > 0 else "L"), outward(tube(pts, [0.0045] * len(pts))), YELLOW, "body",
              fuse=False)
    # Four thin black legs hanging down and back from under the ruff, black balls at their ends
    leg(k, "legfront", [(0.018, -0.0, 0.092), (0.026, 0.012, 0.05), (0.03, 0.03, 0.018)], 0.0042, ball=0.009)
    leg(k, "legback", [(0.022, 0.03, 0.09), (0.034, 0.05, 0.056), (0.042, 0.075, 0.03)], 0.0042, ball=0.009)
    # One pair of broad pale wings raised high and back, a tan loop near each root
    L, W = 0.118, 0.042
    wing(k, WING_ROOT, (0.55, 0.32, 0.77), (0.42, -0.9, -0.1), L, W,
         [(oval(0.02, 0.07, 0.019), LOOP), (oval(0.031, 0.061, 0.009), WING)])
    k.build(voxel=0.0027, smooth=4, band=0.004, wband=0.03)

    def eyes(state):
        lay_eyes(k, HC, HR, EYE_D, EYE_HALF, eye_open(), YELLOW, state)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    cutiefly(*args())
