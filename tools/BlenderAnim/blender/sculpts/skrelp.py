# Skrelp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/skrelp.py -- <the export's Skrelp.glb> <out.glb>
#
# From the references: a small lilac sea horse hidden under a great brown leaf of kelp, hovering. The leaf is a hood
# over the whole head, the largest mass: a cone pointed at the top, its brim spread over the face and out into a
# pointed flap on each side; the face comes out under it, a big red eye with a black pupil whose top the brim cuts,
# and a long lilac snout reaching forward to a round knob of a tip. From the back of the hood a thin brown stalk rises
# to a pale blue drop of a leaf. A slender lilac body under it, its front crossed by darker lines and its back brown,
# a pointed brown leaf of a fin on the back, a small pale blue fin out of each side; low down the body turns brown and
# parts into three short strands, each ending in a lobed brown leaf (the kelp's holdfast, not feet).
#
# Written to be grown from (dragalge.py imports it with sibling): the frame is in the constants and the functions that
# take the kit; the build is under the main guard. Bones: root, body (0, 0, 0.227), head (0, 0, 0.307).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, unit, eye_look, args, slab, polygon

LILAC, LINE, BROWN, DARK, DROP = "#D4A2D6", "#A2709E", "#A36C47", "#7E5034", "#B2D8EE"
RED, DEEP, INK, PUPIL, WHITE = "#DA3038", "#961E2A", "#1E141A", "#120A10", "#FFF4F4"

# ---- the frame ----------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.014, 0.252]), np.array([0.047, 0.052, 0.048])
SNOUT = [(0.0, -0.045, 0.243), (0.0, -0.09, 0.236), (0.0, -0.128, 0.24), (0.0, -0.148, 0.25)]
SNOUT_R = [0.022, 0.0175, 0.017, 0.018]
TIP_C, TIP_R = np.array([0.0, -0.156, 0.252]), np.array([0.024, 0.021, 0.022])
HOOD_C, HOOD_R = np.array([0.0, 0.006, 0.302]), np.array([0.108, 0.104, 0.142])
SPINE = [(0.0, 0.006, 0.225), (0.0, 0.022, 0.185), (0.0, 0.03, 0.14), (0.0, 0.022, 0.1), (0.0, 0.016, 0.075)]
SPINE_R = [0.032, 0.033, 0.03, 0.025, 0.021]


def hood_shape(V, lean=0.25):
    """The kelp hood: a cone pointed at the top, its underside a shallow dome, its tip leaning back a little."""
    V = V.copy()
    z = V[:, 2]
    t = (z + 1) / 2
    s = np.where(t < 0.45, 1.0, np.clip((1 - t) / 0.55, 0, 1) ** 0.62)
    V[:, 0] *= s
    V[:, 1] *= s
    V[:, 1] += lean * np.clip(t - 0.4, 0, 1) ** 2
    V[:, 2] = np.where(z < 0, z * 0.32, z)
    return V


def hood(k, c=HOOD_C, r=HOOD_R, color=BROWN, bone="head", flap=((0.075, 0.004, 0.304), (0.12, -0.002, 0.298),
                                                                 (0.158, -0.008, 0.284)), flap_r=(0.04, 0.024, 0.008)):
    k.add("hood", ell(c, r, deform=hood_shape), color, bone)
    k.add("hoodflap", blade(list(flap), list(flap_r), (0, 0, 1), 0.42), color, bone, mirror=True)


def head(k, c=HEAD_C, r=HEAD_R, color=LILAC):
    k.add("head", ell(c, r), color, "head")
    k.add("jaw", ell(c + (0, -0.018, -0.018), r * (0.78, 0.7, 0.62)), color, "head")


def snout(k, pts=SNOUT, radii=SNOUT_R, tip_c=TIP_C, tip_r=TIP_R, color=LILAC):
    k.add("snout", tube(pts, radii), color, "head")
    k.add("snouttip", ell(tip_c, tip_r), color, "head")


def torso(k, spine=SPINE, radii=SPINE_R, color=LILAC):
    k.add("neck", tube(spine, radii, seg=18, per=6), color, "body")


def spine_y(z, spine=SPINE):
    """The body's centre line's y at a height (the spine's points are in falling z)."""
    s = np.array(spine)
    return np.interp(z, s[::-1, 2], s[::-1, 1])


def back_brown(k, z0=0.068, z1=0.212, low=0.098, color=BROWN, spine=SPINE, off=0.0):
    """The back of the body brown, the front lilac, split down the side; everything under `low` brown."""
    def m(P):
        z = P[:, 2]
        band = inside(z - z1, 0.004) * inside(z0 - z, 0.004)
        back = inside(spine_y(z, spine) + off - P[:, 1], 0.003)
        return np.maximum(band * back, inside(z - low, 0.004) * inside(-0.02 - z, 0.004) * (z > 0.0))
    k.paint(m, color)


def belly_lines(k, rows=(0.118, 0.152, 0.186), half=(0.022, 0.0042), color=LINE, spine=SPINE, radii=SPINE_R):
    """A few darker lines across the lilac front, thin lens patches cast from the front."""
    s, rr = np.array(spine), np.array(radii)
    for z in rows:
        y = spine_y(z, spine) - np.interp(z, s[::-1, 2], rr[::-1])
        k.decal((0, y, z), unit(0, -1, 0.08), half, color, eps=0.003, rings=4, segs=36)


def leaf_outline(L, W, n=16, point=1.0):
    """A leaf from (0, 0) to (L, 0), widest past its middle; point 1 is pointed, less is rounder."""
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((L * t, W * math.sin(math.pi * t ** 0.75) ** point))
    for i in range(n - 1, 0, -1):
        t = i / n
        pts.append((L * t, -W * math.sin(math.pi * t ** 0.75) ** point))
    return np.array(pts)


def oval_outline(a, b, n=24):
    return np.array([(a * math.cos(2 * math.pi * i / n), b * math.sin(2 * math.pi * i / n)) for i in range(n)])


def dorsal(k, root=(0.006, 0.042, 0.13), d=(0.12, 0.45, 0.88), L=0.15, W=0.034, color=BROWN, bone="body"):
    """The pointed leaf of a fin on the back, up and back, its face turned some 35 degrees out to +X."""
    d = unit(*d)
    ez = np.cross(d, unit(-0.57, 0.82, 0))
    ez /= np.linalg.norm(ez)
    if ez[1] < 0:
        ez = -ez
    k.add("dorsal", slab(leaf_outline(L, W, point=1.3), root, d, ez, 0.016), color, bone, fuse=False, surface="scales")


def side_fins(k, root=(0.026, 0.02, 0.122), d=(0.82, 0.38, -0.12), a=0.034, b=0.022, color=DROP, bone="body"):
    """A small pale blue oval fin out of each side, its face to the front."""
    d = unit(*d)
    ez = unit(0, 0, 1.0)
    c = np.array(root) + d * a * 0.9
    k.add("sidefin", slab(oval_outline(a, b), c, d, ez, 0.011), color, bone, fuse=False, mirror=True, surface="scales")


def stalk(k, pts=((0.0, 0.075, 0.33), (0.012, 0.1, 0.36), (0.026, 0.108, 0.39), (0.04, 0.118, 0.405)),
          r=(0.011, 0.009, 0.0085, 0.0085), leaf_c=(0.075, 0.15, 0.415), leaf_d=(0.62, 0.78, 0.1), a=0.052, b=0.028,
          color=BROWN, leaf=DROP, bone="head"):
    """A thin brown stalk out of the hood's back, to a pale blue drop of a leaf run back and out to +X."""
    k.add("stalk", tube(list(pts), list(r), seg=12, per=6), color, bone)
    d = unit(*leaf_d)
    k.add("drop", slab(oval_outline(a, b), leaf_c, d, unit(0, 0, 1.0), 0.012), leaf, bone, fuse=False,
          surface="scales")


def strand(k, name, pts, r, color=BROWN, bone="root"):
    k.add(name, tube(pts, r, seg=12, per=6), color, bone)


def lobed_pad(k, name, c, d, size, color=BROWN, bone="root", lobes=3, spread=55.0, flat=0.009):
    """A lobed leaf lying nearly flat: lobes fanned out along d, each a flattened ellipsoid."""
    d = np.array(d, float); d[2] = 0; d /= np.linalg.norm(d)
    base = math.degrees(math.atan2(d[1], d[0]))
    c = np.array(c, float)
    for i in range(lobes):
        a = math.radians(base + spread * (i - (lobes - 1) / 2) / max(lobes - 1, 1) * 2)
        dd = np.array([math.cos(a), math.sin(a), 0.0])
        p = c + dd * size * 0.75
        k.add(f"{name}{i}", ell(p, (size, size * 0.55, flat), rot=(0, 0, math.degrees(a))), color, bone)
    k.add(f"{name}hub", ell(c, (size * 0.6, size * 0.6, flat * 1.2)), color, bone)


def holdfast(k, top=(0.0, 0.016, 0.07), feet=(((-0.04, -0.034, 0.03), (-0.6, -0.8, 0)),
                                                ((0.044, -0.028, 0.028), (0.7, -0.7, 0)),
                                                ((0.006, 0.058, 0.03), (0.1, 1.0, 0))),
             r=0.0135, size=0.03, color=BROWN):
    top = np.array(top)
    for i, (f, d) in enumerate(feet):
        f = np.array(f)
        mid = top * 0.45 + f * 0.55 + (0, 0, 0.012)
        strand(k, f"strand{i}", [top, mid, f + (0, 0, 0.004)], [r * 1.1, r, r * 0.95], color)
        lobed_pad(k, f"pad{i}", f + np.array(d) * 0.012, d, size, color)


# ---- the eyes: big and red, a black pupil, the hood's brim cutting the top -------------------------------------------
CL, CK, CR, CD, CP, CW = srgb(LILAC), srgb(INK), srgb(RED), srgb(DEEP), srgb(PUPIL), srgb(WHITE)
EYE_N = unit(0.66, -0.74, 0.06)
EYE_HALF = (0.03, 0.031)
TOP = lambda u: 0.46 + 0.18 * u                 # +u runs toward the back: the brim a little lower toward the snout


def skrelp_eye(u, v):
    c = np.tile(CK, (len(u), 1))
    rr = u * u + v * v
    c[rr < 0.74] = CD
    c[rr < 0.5] = CR
    c[((u + 0.06) / 0.25) ** 2 + ((v + 0.03) / 0.4) ** 2 < 1] = CP
    c[((u + 0.24) / 0.12) ** 2 + ((v - 0.2) / 0.12) ** 2 < 1] = CW
    return c


def cut_outline(top=TOP, n=96):
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    u, v = 0.98 * np.cos(t), 0.98 * np.sin(t)
    v = np.minimum(v, top(u))
    return np.stack([u, v], axis=1)


def eye_centre(c=HEAD_C, r=HEAD_R, n=EYE_N, lift=(0, 0, 0.004)):
    return c + n * r * 0.96 + np.array(lift)


def cheek(k, c=HEAD_C, r=HEAD_R, n=EYE_N, color=LILAC, bone="head"):
    """A flattened part under each eye, so the big eye lies on a flat face and does not wrap."""
    p = c + n * r * 0.68
    k.add("cheek", ell(p, r * (0.95, 0.95, 0.98), rot=(0, 0, -math.degrees(math.atan2(n[0], -n[1])))), color, bone,
          mirror=True)


def eyes(k, look=skrelp_eye, centre=None, n=EYE_N, half=EYE_HALF, top=TOP, lid=CL, ink=CK, eps=0.0035):
    c = eye_centre() if centre is None else np.array(centre, float)
    M = np.array([-1.0, 1, 1])
    OUT = cut_outline(top)

    def lay(state):
        f = look if state == "Open" else eye_look(state, look, lid, ink, line=0.17)
        # laid by hand a side, since a mirrored patch's outline is not mirrored (kingdra.py)
        k.decal(c, n, half, f, eps=eps, rings=20, segs=56, outline=polygon(OUT))
        k.decal(c * M, n * M, half, lambda u, v: f(-u, v), eps=eps, rings=20, segs=56,
                outline=polygon(OUT * np.array([-1, 1])))
    return lay


def skrelp(src, out):
    k = Kit(src, coat="scales")
    hood(k)
    head(k)
    cheek(k)
    snout(k)
    torso(k)
    stalk(k)
    holdfast(k)
    back_brown(k)
    belly_lines(k)
    dorsal(k)
    side_fins(k)
    k.build(voxel=0.0042, smooth=3, band=0.005, wband=0.04)
    k.eye_states(eyes(k))
    k.export(out)


if __name__ == "__main__":
    skrelp(*args())
