# Raikou sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raikou.py -- <the export's Raikou.glb> <out.glb>
#
# From the references: a great yellow sabre-toothed cat on thick legs, the trunk deep and heavy, broad shoulders and
# thighs fused over the upper half of each leg, big paws with three cream claws. Black jagged stripes in V's down its
# flanks, shoulders and thighs. A round white ruff fills the cheeks and the back of the head and runs down into a
# white chest; the belly and the backs of the hind thighs are white. The top of the head and the bridge of the snout
# are a dark grey helmet, two white curls standing at its back; the eyes red under the helmet's edge. Over the muzzle a
# great pale blue X, and under it two long cream fangs hanging below the jaw. A purple cloud lies along the back from
# the nape and billows up over the rump in curls. The tail is a blue lightning bolt rising and zigzagging back to a
# blue star.
#
# The frame is written to be grown from (Entei and Suicune import it through sibling("raikou")): every function takes
# the kit and its numbers, and nothing builds unless this file is run itself.
# Bones: body (0, 0.02, 0.46); head (0, -0.24, 0.56); legs (+-0.1, -0.19, 0.42); hind legs (+-0.1, 0.22, 0.42);
# tail (0, 0.29, 0.48).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, sheet, hull, slab, outward, srgb, inside, smoothstep, tri, unit,
                       eye_look, polygon, lumpy, args)

# --- colours ------------------------------------------------------------------------------------------------------------
YELLOW = "#F4CC34"
WHITE = "#EEF0F4"
HELM = "#3C3E46"
PURPLE = "#A98AB8"
BLUE = "#7CCAEC"
FANG = "#F2EBD2"
STRIPE = "#22242A"
CLAW = "#F2EBD2"
EYE, IRIS, PUPIL = "#F6F4EE", "#D6262E", "#1A1012"


# --- small tools ----------------------------------------------------------------------------------------------------------
def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0035):
    """A polygon painted with a soft edge: the signed distance to its outline, eased."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def on_ell(c, r, d):
    """The point of an ellipsoid along direction d from its centre, and its normal there."""
    d = unit(*d)
    s = 1 / math.sqrt(((d / np.array(r)) ** 2).sum())
    p = np.array(c) + d * s
    n = (p - np.array(c)) / np.array(r) ** 2
    return p, n / np.linalg.norm(n)


def plate(c, r, keep, lift=0.022, under=0.006, n=900, grow=1.0):
    """
    A hard plate hugging an ellipsoid (a face plate, a helmet's ridge): the convex hull of the ellipsoid's points that
    keep(d) picks (d the unit direction from the centre) laid a little under the surface and lifted off it. Convex
    because the ellipsoid is, so the hull's inner face follows the head and its outer face stands proud with hard rims.
    """
    rs = np.random.RandomState(7)
    D = rs.normal(size=(n * 8, 3)); D /= np.linalg.norm(D, axis=1, keepdims=True)
    D = D[keep(D)][:n]
    r = np.array(r) * grow
    S = 1 / np.sqrt(((D / r) ** 2).sum(1))
    P = np.array(c) + D * S[:, None]
    N = (P - np.array(c)) / r ** 2; N /= np.linalg.norm(N, axis=1, keepdims=True)
    return hull(np.vstack([P - N * under, P + N * lift]), merge=6.0)


def lens(n=12):
    """A slender pointed stroke's outline (a lightning stripe, a diamond drawn long)."""
    return polygon([(-0.55, -1.0), (0.55, -1.0), (1.0, 0.0), (0.55, 1.0), (-0.55, 1.0), (-1.0, 0.0)])


STROKE = polygon([(0.0, -1.0), (1.0, -0.35), (1.0, 0.35), (0.0, 1.0), (-1.0, 0.35), (-1.0, -0.35)])


def vee(k, c, normal, size, color, spread=0.55, mirror=True, w=0.02):
    """A V of two pointed strokes meeting at the bottom, laid on the surface at c (a jagged stripe)."""
    c = np.array(c, float)
    n = unit(*normal)
    for s in (1, -1):
        # The stroke leans toward its own side; the V's arms meet at its bottom point
        side = np.cross((0, 0, 1.0), n); side /= np.linalg.norm(side)
        up = unit(*(np.array((0, 0, 1.0)) + side * s * spread))
        k.decal(c + side * s * size * 0.32 + np.array((0, 0, size * 0.45)), n, (w, size * 0.62), color, up=up,
                eps=0.003, outline=STROKE, rings=10, segs=40, mirror=mirror)


# --- the trunk ------------------------------------------------------------------------------------------------------------
def add_trunk(k, color, z=0.43, w=0.14, deep=0.14, front=-0.14, back=0.19, bib=None, bib_color=None):
    """A deep trunk: chest, middle and rump fused, the chest the deepest; bib a fused front of its own colour."""
    k.add("chest", ell((0, front, z), (w, 0.13, deep)), color, "body")
    k.add("middle", ell((0, (front + back) / 2 + 0.005, z - 0.01), (w - 0.01, 0.17, deep * 0.9)), color, "body")
    k.add("rump", ell((0, back, z), (w - 0.005, 0.115, deep * 0.95)), color, "body")
    if bib is not None:
        k.add("bib", ell(bib[0], bib[1], deform=lumpy(4, 3, 0.05)), bib_color, "body")


def leg_radius(z, top, r):
    return np.interp(z, [0.03, top * 0.5, top], [r[2], r[1], r[0]])


def add_legs(k, color, x, fore_y, hind_y, top=0.42, r=(0.07, 0.056, 0.046), shoulder=(0.075, 0.09, 0.14),
             thigh=(0.085, 0.105, 0.15), paw=(0.07, 0.082, 0.037), paw_color=None, lower=None, lower_color=None,
             bend=None, ankle=None):
    """
    bend: how far back the elbow and the hock stand (a leg that tapers and bends to a slim ankle of radius ankle);
    with none, the straight tapering tube Entei keeps.
    Four legs that are no stilts: a broad shoulder and thigh mass fused over the upper half of each leg into the trunk,
    the lower leg a tube tapering to a broad paw. lower: (z) where a lower leg of another colour begins (the hind legs).
    """
    paw_color = paw_color or color
    k.add("shoulder", ell((x - 0.006, fore_y + 0.004, top - shoulder[2] * 0.42), shoulder), color, "leg{s}", mirror=True)
    if bend is None:
        k.add("leg", tube([(x, fore_y, top), (x, fore_y - 0.004, top * 0.5), (x, fore_y - 0.008, 0.05)], list(r)),
              color, "leg{s}", mirror=True)
    else:
        # A cat's or a deer's foreleg: the elbow back under the shoulder's mass, the forearm tapering forward and down
        # to a slim wrist over the paw
        k.add("leg", tube([(x, fore_y, top), (x, fore_y + bend, top * 0.6), (x, fore_y - 0.004, top * 0.3),
                           (x, fore_y - 0.012, 0.11), (x, fore_y - 0.016, 0.055)],
                          [r[0], r[1], (r[1] + ankle) / 2, ankle, ankle * 1.08]), color, "leg{s}", mirror=True)
    k.add("paw", ell((x, fore_y - 0.024, paw[2]), paw), paw_color, "leg{s}", mirror=True)
    k.add("thigh", ell((x - 0.006, hind_y + 0.012, top - thigh[2] * 0.36), thigh), color, "hind{s}", mirror=True)
    if bend is None:
        k.add("hind", tube([(x, hind_y + 0.01, top), (x, hind_y + 0.03, top * 0.5), (x, hind_y + 0.004, 0.05)], list(r)),
              lower_color or color, "hind{s}", mirror=True)
    else:
        # The hind leg: the knee forward under the thigh, the hock well back, the shank slim and upright to the paw
        k.add("hind", tube([(x, hind_y + 0.01, top), (x, hind_y - 0.02, top * 0.62), (x, hind_y + bend * 1.4, top * 0.32),
                            (x, hind_y + 0.008, 0.11), (x, hind_y - 0.004, 0.055)],
                           [r[0], r[1], ankle * 1.2, ankle, ankle * 1.08]), lower_color or color, "hind{s}", mirror=True)
    k.add("hpaw", ell((x, hind_y - 0.02, paw[2]), paw), paw_color, "hind{s}", mirror=True)


def add_claws(k, color, x, fore_y, hind_y, paw=(0.07, 0.082, 0.037), size=1.0):
    """Three cream claws on the front of each paw: flat-faced hulls, a kite at their roots."""
    for bone, y in (("leg{s}", fore_y - 0.024), ("hind{s}", hind_y - 0.02)):
        for j, dx in enumerate((-0.03, 0.0, 0.03)):
            a = np.array([x + dx * size, y - paw[1] * 0.98, 0.024 * size])
            s = size
            pts = [a + np.array(p) * s for p in ((-0.012, 0.018, -0.012), (0.012, 0.018, -0.012), (-0.011, 0.018, 0.012),
                                                 (0.011, 0.018, 0.012), (0, -0.016, -0.004), (0, -0.006, -0.02))]
            k.add(f"claw{bone[0]}{j}", hull(pts), color, bone, fuse=False, mirror=True, surface="shell")


# --- the eyes: a brow (or a plate's edge) cutting the top, sloping down toward the nose ---------------------------------
def clipped(top, n=96):
    pts = []
    for th in np.linspace(0, 2 * math.pi, n, endpoint=False):
        du, dv = math.cos(th), math.sin(th)
        t = 1.0
        if dv > top(du):
            lo, hi = 0.0, 1.0
            for _ in range(40):
                t = (lo + hi) / 2
                if t * dv > top(t * du):
                    hi = t
                else:
                    lo = t
            t = lo
        pts.append((t * math.cos(th), t * math.sin(th)))
    return pts


def eye_open(white, iris, pupil, iris_r=(0.7, 0.86), pupil_r=0.52, at=(-0.18, -0.1)):
    CW, CI, CP = srgb(white), srgb(iris), srgb(pupil)

    def look(u, v):
        c = np.tile(CW, (len(u), 1))
        r = np.sqrt(((u - at[0]) / iris_r[0]) ** 2 + ((v - at[1]) / iris_r[1]) ** 2)
        c[r < 1] = CI
        c[np.sqrt(((u - at[0] - 0.02) / (iris_r[0] * 0.55)) ** 2 + ((v - at[1]) / (iris_r[1] * 0.75)) ** 2) < pupil_r * 1.6]\
            = CP
        c[((u - at[0] + 0.2) / 0.2) ** 2 + ((v - at[1] - 0.25) / 0.2) ** 2 < 1] = (1, 1, 1)
        return c
    return look


def lay_eyes(k, state, centre, direction, radius, look, lid, line, top):
    """Both eyes in a state: outlines cut by top(u) where open, the left's colour and outline flipped."""
    f = eye_look(state, look, srgb(lid), srgb(line), line=0.15)
    out = clipped(top)
    for side in (1, -1):
        n = np.array(direction) * np.array([side, 1, 1.0])
        outline = polygon([(side * a, b) for a, b in out]) if state in ("Open", "Fierce") else None
        col = f if side == 1 else (lambda a, b, g=f: g(-a, b))
        k.decal(np.array(centre) * np.array([side, 1, 1.0]), n, (radius, radius * 0.9), col, eps=0.004,
                outline=outline, rings=20, segs=56)


# ======================================================================================================================
# Raikou's own
# ======================================================================================================================
LEG_X = 0.1
FORE_Y, HIND_Y = -0.19, 0.22
LEG_TOP = 0.42
BODY_Z = 0.43
H = np.array([0.0, -0.3, 0.635])
HR = np.array([0.122, 0.125, 0.112])
EYE_DIR = unit(0.56, -0.8, 0.16)
EYE_R = 0.03
E = H + EYE_DIR * HR[0] * 0.93 + np.array([0, -0.006, 0.0])


def eye_top(u):
    return 0.28 + 0.45 * u


def helmet_edge(P):
    """Under this height a point of the head is white: the helmet's lower edge, low at the nose, up behind the eye."""
    return H[2] - 0.008 + 0.32 * (P[:, 1] - (E[1]))


def add_head(k):
    k.add("head", ell(H, HR), HELM, "head")
    k.add("snout", ell(H + (0, -0.1, -0.02), (0.075, 0.08, 0.062)), HELM, "head")
    k.add("jaw", ell(H + (0, -0.075, -0.075), (0.068, 0.065, 0.042)), WHITE, "head")
    # The ruff: a round white mass behind and round the cheeks, wider than the head, down into the chest
    k.add("ruff", ell(H + (0, 0.06, -0.035), (0.165, 0.12, 0.14), deform=lumpy(5, 3, 0.05)), WHITE, "head")
    k.add("cheek", ell(H + (0.105, -0.01, -0.06), (0.06, 0.07, 0.065)), WHITE, "head", mirror=True)
    k.add("neck", tube([(0, -0.17, 0.48), (0, -0.23, 0.55), (0, -0.28, 0.6)], [0.12, 0.11, 0.1]), WHITE, "head")
    # The helmet's two white curls at its back
    for s in (1, -1):
        k.add(f"curl{'R' if s > 0 else 'L'}", ell(H + (0.04 * s, 0.07, 0.1), (0.042, 0.045, 0.04)), WHITE, "head")
    # The helmet's own edge against the ruff, crisp: the head's and the snout's shapes a little grown, painted over
    # the voxel blend where they meet the white
    def helm(P):
        a = np.linalg.norm((P - H) / (HR * 1.05), axis=1) - 1
        s = np.linalg.norm((P - (H + (0, -0.1, -0.02))) / (np.array((0.075, 0.08, 0.062)) * 1.08), axis=1) - 1
        curls = (P[:, 1] > H[1] + 0.035) & (P[:, 2] > H[2] + 0.06)
        return inside(np.minimum(a, s) * 0.12, 0.003) * ~curls
    k.paint(helm, HELM)
    # White under the helmet's edge on the head and the snout (crisp: no on, kept to the head by place)
    def under(P):
        near = (np.linalg.norm((P - H) / (0.2, 0.22, 0.2), axis=1) < 1) & (P[:, 1] < H[1] + 0.05)
        return inside(P[:, 2] - helmet_edge(P), 0.0025) * near
    k.paint(under, WHITE)


def add_helmet_ridge(k):
    """A hard ridge down the helmet's middle from the crown to the snout's point."""
    k.add("ridge", plate(H + (0, -0.02, 0.0), HR * (1.0, 1.0, 1.0),
                         lambda D: (np.abs(D[:, 0]) < 0.18) & (D[:, 2] > 0.35) & (D[:, 1] < 0.55),
                         lift=0.018, under=0.012), HELM, "head", fuse=False, surface="shell")


def add_x(k):
    """The pale blue X over the muzzle: a thick four-pointed star facing forward, a little up."""
    pts = []
    for i in range(8):
        a = math.radians(45 + 45 * i)
        if i % 2 == 0:
            ln = 0.105 if math.sin(a) < 0 else 0.09
            pts.append((ln * math.cos(a), ln * math.sin(a)))
        else:
            pts.append((0.026 * math.cos(a), 0.026 * math.sin(a)))
    o = H + (0, -0.172, -0.03)
    k.add("x", slab(pts, o, (1, 0, 0), unit(0, -0.08, 1), 0.026), BLUE, "head", fuse=False, surface="shell")


def add_fangs(k):
    for s in (1, -1):
        a = H + (0.033 * s, -0.16, -0.075)
        pts = [a + (0.014, 0.012, 0.0), a + (-0.014, 0.012, 0.0), a + (0.014, -0.012, 0.0), a + (-0.014, -0.012, 0.0),
               a + (0.002 * s, -0.012, -0.19), a + (0.004 * s, -0.004, -0.2)]
        k.add(f"fang{'R' if s > 0 else 'L'}", hull(pts), FANG, "head", fuse=False, surface="shell")


def add_cloud(k):
    """The purple cloud: broad lumpy masses from the nape along the back, billowing up over the rump in curls."""
    for i, (c, r) in enumerate((((0, -0.2, 0.6), (0.11, 0.09, 0.07)), ((0, -0.08, 0.61), (0.14, 0.11, 0.075)),
                                ((0, 0.05, 0.62), (0.15, 0.11, 0.08)), ((0, 0.17, 0.675), (0.14, 0.1, 0.105)))):
        k.add(f"cloud{i}", ell(c, r, deform=lumpy(4, 2, 0.06)), PURPLE, "body")
    # The billow's curls: rolled balls on each side and one behind
    for s in (1, -1):
        k.add(f"cloudcurl{'R' if s > 0 else 'L'}", ell((0.1 * s, 0.22, 0.67), (0.065, 0.075, 0.07)), PURPLE, "body")
    k.add("cloudback", ell((0, 0.27, 0.64), (0.09, 0.06, 0.075)), PURPLE, "body")


def add_belly(k):
    """White under the belly and behind the hind thighs, its edge jagged like fur."""
    def belly(P):
        zig = 0.012 * tri(P[:, 1] / 0.05)
        return inside(P[:, 2] - (0.33 + zig), 0.003) * inside(np.abs(P[:, 0]) - 0.075, 0.004) * (P[:, 2] > 0.2)
    k.paint(belly, WHITE, on=["chest", "middle", "rump", "bib"])


def add_stripes(k):
    """Black jagged V's down the flanks, on the shoulders and on the thighs."""
    for y, z in ((-0.02, 0.41), (0.12, 0.41)):
        vee(k, (0.12, y, z), (1, 0, 0.2), 0.14, STRIPE, spread=0.8, w=0.026)
    vee(k, (LEG_X + 0.07, FORE_Y, 0.29), (1, -0.25, 0), 0.11, STRIPE, spread=0.7, w=0.022)
    vee(k, (LEG_X + 0.08, HIND_Y + 0.01, 0.3), (1, 0.15, 0), 0.12, STRIPE, spread=0.7, w=0.024)


def add_tail(k):
    """The blue lightning bolt: thick slabs zigzagging up and back from the rump, out toward +X, to a star."""
    ex = unit(0.85, 0.53, 0)
    o = np.array((0.0, 0.29, 0.5))
    path = [(0.0, 0.0), (0.05, 0.3), (0.1, 0.13), (0.16, 0.25)]
    w = 0.019
    for i, (a, b) in enumerate(zip(path, path[1:])):
        a, b = np.array(a), np.array(b)
        d = (b - a) / np.linalg.norm(b - a); nn = np.array((-d[1], d[0]))
        a0, b0 = a - d * 0.012, b + d * 0.012
        out = [a0 + nn * w, b0 + nn * w, b0 - nn * w, a0 - nn * w]
        k.add(f"bolt{i}", slab(out, o, ex, (0, 0, 1), 0.026 + 0.002 * i), BLUE, "tail", fuse=False, surface="shell")
    tip = np.array(path[-1])
    star = []
    for i in range(10):
        a = math.radians(90 + 36 * i)
        rr = 0.055 if i % 2 == 0 else 0.022
        star.append((tip[0] + 0.024 + rr * math.cos(a), tip[1] + rr * math.sin(a)))
    k.add("star", slab(star, o, ex, (0, 0, 1), 0.034), BLUE, "tail", fuse=False, surface="shell")


EYE_LID = HELM
SHUT_LINE = "#D8DCE4"


def eyes(k, state):
    lay_eyes(k, state, E, EYE_DIR, EYE_R, eye_open(EYE, IRIS, PUPIL), EYE_LID, SHUT_LINE, eye_top)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_trunk(k, YELLOW, z=BODY_Z, deep=0.15, bib=((0, -0.26, 0.45), (0.115, 0.08, 0.14)), bib_color=WHITE)
    add_legs(k, YELLOW, LEG_X, FORE_Y, HIND_Y, LEG_TOP, r=(0.07, 0.052, 0.046), bend=0.035, ankle=0.027)
    add_head(k)
    add_cloud(k)
    add_belly(k)
    add_stripes(k)
    add_claws(k, CLAW, LEG_X, FORE_Y, HIND_Y)
    add_x(k)
    add_fangs(k)
    add_tail(k)
    k.build(voxel=0.008, band=0.004)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)
