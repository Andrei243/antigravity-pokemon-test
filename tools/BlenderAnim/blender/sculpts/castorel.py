# Castorel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/castorel.py -- <the export's Castorel.glb> <out.glb>
#
# Carpathia No. 10149, a beaver kit (CP-dex-3b-river-and-caves-more.md, 149; picture 10149-Castorel.jpg).
# The masses, largest first (picture): a chubby pear of a body with a lighter belly; the big round head (0.42 of the
# height, 0.72 of the body's width) on it with no neck to speak of; the flat dark teal paddle tail, scaled, resting
# behind (back and out to +X, so the front camera and the back one both see its face); broad dark teal webbed feet;
# the twig gnawed in both paws at the chin, its long end to +X; small round ears.
# The face: big round eyes (a teal iris round a black pupil, two white glints), a large dark teal nose, a smile, two
# bright orange front teeth over the twig. Teal scales on the +X hip. The twig is kept under the teeth and the paws
# under the chin, so nothing covers the face.
# Skeleton: body (0, 0, 0.2), head (0, -0.02, 0.32), arms (+-0.11, -0.06, 0.27), legs (+-0.08, -0.01, 0.1),
# tail (0, 0.12, 0.08). Export 0.53 tall.
#
# Castoroi (castoroi.py) grows from this file: the palette, on_ell, the eye, the teeth, the nose, the scales, the paddle.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, srgb, inside, unit, eye_look, polygon, outward, args

# --- the line's palette ----------------------------------------------------------------------------------------------
RUSSET = "#A2523E"
BELLY = "#BE7058"
EAR_IN = "#8A4436"
TEAL = "#2C5257"
TEAL_DARK = "#1E3A3E"
SCALE = "#3C7A7C"
TOOTH = "#F2861E"
NOSE = "#1F343A"
TWIG, TWIG_DARK = "#7A5638", "#5A3E28"
INK = srgb("#20161A")
IRIS, PUPIL, GLINT = srgb("#3E7C86"), srgb("#14181C"), srgb("#FFFFFF")


def on_ell(c, r, d):
    d = np.array(d, float); d /= np.linalg.norm(d)
    c, r = np.array(c, float), np.array(r, float)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def eye_open(glint=1.0):
    """The line's eye: a teal iris round a big black pupil, a large white glint high and outward and a small one low."""
    def look(u, v):
        c = np.tile(INK, (len(u), 1))
        c[(u / 0.88) ** 2 + (v / 0.9) ** 2 < 1] = IRIS
        c[(u / 0.56) ** 2 + ((v + 0.04) / 0.6) ** 2 < 1] = PUPIL
        c[((u - 0.22) / (0.3 * glint)) ** 2 + ((v - 0.3) / (0.28 * glint)) ** 2 < 1] = GLINT
        c[((u + 0.28) / (0.13 * glint)) ** 2 + ((v + 0.38) / (0.12 * glint)) ** 2 < 1] = GLINT
        return c
    return look


def scales(rows, cols, ink, ground, width=0.1, mask=None):
    """A colour function of overlapping scales (rows of arcs, each row half a scale over), in a patch's u, v."""
    def look(u, v):
        c = np.tile(srgb(ground), (len(u), 1))
        q = (v + 1) / 2 * rows
        row = np.floor(q)
        off = 0.5 * (row % 2)
        cu = (u + 1) / 2 * cols + off
        fx = cu - np.round(cu)
        fy = q - row
        d = np.sqrt(fx * fx + fy * fy)
        hit = (np.abs(d - 0.5) < width) & (fy > 0.02)
        if mask is not None:
            hit &= mask(u, v)
        c[hit] = srgb(ink)
        return c
    return look


def teeth(k, centre, normal, up, half_w, height, depth, bone):
    """Two bright orange chisel teeth standing down from under the nose (loose hulls)."""
    n = np.array(normal, float); n /= np.linalg.norm(n)
    up = np.array(up, float); up = up - n * (up @ n); up /= np.linalg.norm(up)
    side = np.cross(up, n)
    for s in (1, -1):
        c = np.array(centre, float) + side * s * (half_w + 0.0015)
        pts = []
        for a in (-1, 1):
            for b in (0, 1):
                for e in (-1, 1):
                    pts.append(c + side * a * half_w * (0.95 if b else 1.0) - up * b * height + n * e * depth)
        k.add(f"tooth{s}", hull(pts), TOOTH, bone, fuse=False, surface="shell")


def twig(k, a, b, r, bone, side_at=0.3, side_d=(0, -0.3, 1)):
    """A gnawed twig from a to b with a short side branch."""
    a, b = np.array(a, float), np.array(b, float)
    k.add("twig", tube([a, (a + b) / 2 + np.array([0, 0, 0.004]), b], [r, r * 1.05, r * 0.9]), TWIG, bone, fuse=False,
          surface="leather")
    s0 = a + (b - a) * side_at
    d = unit(*side_d)
    k.add("twigside", tube([s0, s0 + d * r * 2.4, s0 + d * r * 4.2], [r * 0.75, r * 0.65, r * 0.55]), TWIG, bone,
          fuse=False, surface="leather")
    for i, t in enumerate((0.08, 0.92)):
        p = a + (b - a) * t
        k.add(f"twigend{i}", ell(p + (b - a) / np.linalg.norm(b - a) * (r * 0.9 if t > 0.5 else -r * 0.9),
                                 (r * 0.5, r * 0.5, r * 0.5)), TWIG_DARK, bone, fuse=False, surface="leather")


# --- Castorel's frame ------------------------------------------------------------------------------------------------
FACE = unit(0, -1, 0.04)
HUP = unit(0, 0.04, 1)
X = np.array([1.0, 0, 0])
HC = np.array([0.0, -0.04, 0.408])
HR = np.array([0.142, 0.126, 0.126])


def hd(a, f, u):
    return a * X + f * FACE + u * HUP


MC = HC + FACE * 0.1 - HUP * 0.045
MR = np.array([0.068, 0.05, 0.05])


def castorel(src, out):
    k = Kit(src, coat="fur")
    # The pear of a body, the big head on it
    k.add("hips", ell((0, 0.005, 0.165), (0.178, 0.152, 0.165)), RUSSET, "body")
    k.add("chest", ell((0, -0.012, 0.285), (0.148, 0.128, 0.12)), RUSSET, "body")
    k.add("head", ell(HC, HR), RUSSET, "head")
    k.add("muzzle", ell(MC, MR), RUSSET, "head")
    for i, (a, f, u, ln) in enumerate(((0.92, 0.25, -0.35, 0.032), (0.85, 0.2, -0.62, 0.028))):
        p, n = on_ell(HC, HR, hd(a, f, u))
        d = unit(*(n + HUP * -0.4 + FACE * 0.2))
        k.add(f"tuft{i}", tube([p - n * 0.012, p + d * ln * 0.6, p + d * ln], [0.022, 0.013, 0.004]), RUSSET, "head",
              mirror=True)
    ep, en = on_ell(HC, HR, hd(0.6, 0.12, 0.76))
    k.add("ear", ell(ep - en * 0.006, (0.034, 0.026, 0.034), rot=(0, -30, 0)), RUSSET, "head", mirror=True)
    # Short thick legs, broad flat webbed feet, three toes each
    k.add("leg", ell((0.088, -0.02, 0.08), (0.065, 0.07, 0.07)), RUSSET, "leg{s}", mirror=True)
    k.add("foot", ell((0.1, -0.07, 0.018), (0.06, 0.085, 0.018), rot=(0, 0, 12)), TEAL, "leg{s}", mirror=True)
    for j in range(3):
        a = math.radians(-28 + j * 28)
        p = np.array([0.1, -0.07, 0.016]) + np.array([math.sin(a) * 0.075 + 0.012, -math.cos(a) * 0.08, 0])
        k.add(f"toe{j}", ell(p, (0.017, 0.026, 0.015), rot=(0, 0, -math.degrees(a) + 12)), TEAL, "leg{s}", mirror=True)
    # The arms up to the chin, the paws round the twig
    k.add("arm", tube([(0.115, -0.06, 0.27), (0.12, -0.14, 0.275), (0.08, -0.19, 0.296)], [0.034, 0.031, 0.029]),
          RUSSET, "arm{s}", mirror=True)
    k.add("paw", ell((0.064, -0.208, 0.3), (0.03, 0.026, 0.028)), RUSSET, "arm{s}", mirror=True)
    twig(k, (-0.12, -0.225, 0.292), (0.21, -0.22, 0.286), 0.016, "head", side_at=0.86, side_d=(0.45, -0.35, 0.8))
    # The paddle tail: flat, scaled, resting behind, back and out to +X
    k.add("tailroot", tube([(0, 0.1, 0.1), (0.03, 0.17, 0.05)], [0.055, 0.04]), RUSSET, "tail")
    TC, TR, TROT = np.array([0.085, 0.3, 0.032]), (0.088, 0.165, 0.022), (4, 0, -28)
    k.add("tail", ell(TC, TR, rot=TROT), TEAL, "tail", surface="scales")
    # The nose
    npt, nn = on_ell(MC, MR, hd(0, 0.8, 0.6))
    k.add("nose", ell(npt + nn * 0.002, (0.032, 0.02, 0.022), rot=(-20, 0, 0), deform=lambda V: V * np.array(
        [1, 1, 1.0])[None] * (1 + 0.25 * np.clip(V[:, 2:3], 0, 1))), NOSE, "head", fuse=False, surface="skin")
    k.add("noseglint", ell(npt + nn * 0.012 + HUP * 0.006 + X * 0.008, (0.009, 0.004, 0.005)), "#5E7E86", "head",
          fuse=False, surface="skin")
    mp, mn = on_ell(MC, MR, hd(0, 0.62, -0.78))
    teeth(k, mp + mn * 0.008, unit(0, -1, -0.2), HUP, 0.012, 0.036, 0.007, "head")

    # Paint: the lighter belly, the ears' insides, the soles darker
    def belly(P):
        q = P - np.array([0, -0.1, 0.19])
        return inside((q[:, 0] / 0.12) ** 2 + (q[:, 2] / 0.15) ** 2 - 1, 0.08) * inside(P[:, 1] + 0.06, 0.02)
    k.paint(belly, BELLY, on=["hips", "chest"])
    k.paint(lambda P: inside(P[:, 2] - 0.01, 0.003), TEAL_DARK, on=["foot", "toe"])
    # The ears' insides, the smile, the scales on the tail and on the +X hip
    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        k.decal((ep - en * 0.006) * m + unit(0, -1, 0.2) * 0.02, unit(0.3 * s, -1, 0.2), (0.016, 0.018), EAR_IN,
                eps=0.003, rings=6, segs=24)
    mouth = polygon([(-1, 0.5), (0, -0.3), (1, 0.5), (0.85, 0.85), (0, 0.2), (-0.85, 0.85)])
    k.decal(mp + HUP * 0.012 + X * 0.022, mn, (0.016, 0.012), INK, up=HUP, eps=0.003, outline=mouth, rings=6, segs=36)
    k.decal(mp + HUP * 0.012 - X * 0.022, mn, (0.016, 0.012), INK, up=HUP, eps=0.003, outline=mouth, rings=6, segs=36)
    tn = unit(0.05, 0.07, 1)
    k.decal(TC + np.array([0, 0, 0.022]), tn, (0.15, 0.08), scales(6, 3, SCALE, TEAL, 0.11), up=unit(0.47, 0.88, 0),
            eps=0.003, rings=18, segs=48)
    hip = np.array([0.155, 0.06, 0.16])
    k.decal(hip, unit(1, 0.35, -0.05), (0.055, 0.06), scales(3, 3, SCALE, RUSSET, 0.12,
            mask=lambda u, v: (u * u + v * v) < 0.8), up=(0, 0, 1), eps=0.003, rings=12, segs=40)
    k.build(voxel=0.0052, smooth=4, band=0.006, wband=0.04)
    board(k.ob)

    ep2, en2 = on_ell(HC, HR, hd(0.43, 0.84, 0.25))
    look = eye_open()
    LID = srgb(RUSSET)
    def lay(state):
        if state == "Fierce":
            def f(u, v):
                c = look(u, v)
                c[v > 0.15 + 0.45 * u] = LID
                c[np.abs(v - (0.15 + 0.45 * u)) < 0.12] = INK
                return c
            col = f
        else:
            col = eye_look(state, look, LID, INK, line=0.18)
        k.decal(ep2, en2, (0.03, 0.035), col, up=HUP, eps=0.0026, rings=20, segs=56, mirror=True)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    castorel(*args())
