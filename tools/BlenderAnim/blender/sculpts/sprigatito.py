# Sprigatito sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sprigatito.py -- <the export's Sprigatito.glb> <out.glb>
# From the references: a small pale mint-green kitten on four short legs with a huge round head (half its height);
# large pointed ears, bright green with pale linings; a bright green mask over the face like a leaf's two halves, its
# top rising to the ears' roots in two corners with the pale forehead coming down in a point between them, its bottom
# coming to a point at the little pink nose; big round eyes, pink irises in white, black pupils; an open lavender mouth
# with two little fangs; great pale tufts of fur flaring out from the cheeks; a collar of bright green leaves at the
# throat with a pale leaf in the middle; a big fluffy tail curling up behind, greyer toward its end.
# (The pictures overrule the task's text: the mask and ears are the bright green of a fresh leaf, not a dark green;
# the ruff on the face is pale fur, the leafy collar below it green.)
# Reference ratios (3D picture): head without ears / height 0.52 (here 0.49), head width / body width about 3 (here
# 2.2: the body is kept thick enough to read), tufts' span / head width 1.37 (here 1.36), leg showing under the belly /
# height 0.2 (here 0.17).
#
# Written to be grown from (floragato.py, meowscarada.py keep its colours, its head coordinates, its eye and its mask's
# way of painting). The skeleton's joints: body (0, 0.01, 0.15), head (0, -0.04, 0.23), ears (+-0.06, -0.04, 0.34),
# legs (+-0.045, -0.05, 0.12), hind legs (+-0.045, 0.07, 0.12), tail (0, 0.1, 0.18). About 0.45 tall.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, leaf, outward, srgb, inside, smoothstep, unit, eye_look, polygon, axes,
                       boxy, args)

# --- colours ------------------------------------------------------------------------------------------------------------
PALE = "#CDE9B6"          # the coat: a pale mint green
BRIGHT = "#5DBB3C"        # the mask and the ears: a fresh leaf's green
LEAFDARK = "#3C9A30"      # the collar's leaves
LINING = "#DAF0C6"
GREY = "#9FB596"          # the tail's greyer end
PINK, PINKDARK = "#E8809A", "#C24C6E"
NOSE = "#E8708E"
MOUTH, MOUTHRIM = "#C39AD6", "#5E3150"
INK = "#1A2A14"
WHITE = "#FFFFFF"

# --- the frame ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.062, 0.282])        # the head's middle, up and forward of its joint
HR = np.array([0.122, 0.1, 0.112])        # very big and round, a little wider than tall
TAIL_OUT = unit(0.57, 0.82, 0)            # the tail's sweep: back and some 35 degrees out to +X


# --- shared helpers -----------------------------------------------------------------------------------------------------
def on_head(x, z, h, hr, lift=0.0):
    """The head's front point at (x, z) on its ellipsoid, lifted along its normal, and the normal."""
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / np.array(hr) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def round_head(P, h, hr):
    """Coordinates round the head's upright axis: the signed arc out from the face's middle (metres) and height."""
    return np.arctan2(P[:, 0], -(P[:, 1] - h[1])) * hr[0], P[:, 2] - h[2]


def in_poly(x, y, pts):
    pts = np.array(pts, float)
    res = np.zeros(len(x), bool)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        res ^= ((y1 > y) != (y2 > y)) & (x < (x2 - x1) * (y - y1) / (y2 - y1 + 1e-12) + x1)
    return res


def poly_mask(x, y, pts, edge=0.0015):
    """A soft-edged mask of a polygon in a plane of two coordinates."""
    pts = np.array(pts, float)
    d = np.full(len(x), 9.0)
    for (x1, y1), (x2, y2) in zip(pts, np.roll(pts, -1, axis=0)):
        ex, ey = x2 - x1, y2 - y1
        t = np.clip(((x - x1) * ex + (y - y1) * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(x - x1 - t * ex, y - y1 - t * ey))
    return inside(np.where(in_poly(x, y, pts), -d, d), edge)


def mirrored(half):
    """A polygon symmetric about s = 0 from its right half, walked from the top of the middle round to the bottom."""
    return half + [(-a, z) for a, z in half[::-1][1:-1]]


def spike(base, d, length, width, depth=None):
    """A solid point of fur, flat-faced, from a diamond of a root along d."""
    d = unit(*d)
    depth = depth or width * 0.6
    a, b = axes(d)
    base = np.array(base, float)
    return hull([base + a * width, base - a * width, base + b * depth, base - b * depth, base - d * width * 0.5,
                 base + d * length])


def ear(k, root, tip, base, depth, color, lining, bone, name, lining_share=0.6, face_up=0.15):
    """A large pointed ear: a fat solid triangle from root to tip, a pale lining just proud of its front."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root
    L = np.linalg.norm(d); d = d / L
    across = unit(*np.cross(d, (0, 1.0, 0)))
    face = unit(0, -1, face_up)
    face = unit(*(face - d * (face @ d)))
    dp = base * depth

    def prof(t):
        if t <= 0.4:
            f = t / 0.4
            return base * (1 - 0.35 * f), dp * (1 - 0.2 * f)
        f = (t - 0.4) / 0.6
        return base * 0.65 * (1 - f), dp * 0.8 * (1 - f)

    pts = []
    for t in (0.0, 0.4):
        w, f = prof(t)
        c = root + d * L * t
        pts += [c + across * w + face * f, c - across * w + face * f, c + across * w - face * f,
                c - across * w - face * f]
    pts.append(tip)
    k.add(name, hull(pts), color, bone, fuse=False, surface="fur")
    lp = []
    for t in (0.12, 0.42, 0.78):
        w, f = prof(t)
        w *= lining_share * (1 if t < 0.7 else 0.55)
        c = root + d * L * t
        for off in (f + dp * 0.12, f - dp * 0.35):
            lp += [c + across * w + face * off, c - across * w + face * off]
    lp.append(root + d * L * 0.9 + face * (prof(0.9)[1] + dp * 0.08))
    k.add(name + "in", hull(lp), lining, bone, fuse=False, surface="fur")


def flat_leaf(k, name, base, d, length, width, color, bone, thick, up=(0, 0, 1)):
    """A loose leaf, wound outward."""
    k.add(name, outward(leaf(base, d, length, width, thick)), color, bone, fuse=False, surface="leaf")


def mask_paint(k, poly, color, h=H, hr=HR, on=("head", "muzzle", "cheek"), front=0.2, edge=0.003):
    """A mask painted on the face in coordinates round the head's upright axis (arc, height from the middle)."""
    def m(P):
        s, z = round_head(P, h, hr)
        return poly_mask(s, z, poly, edge) * (P[:, 1] < h[1] - hr[1] * front)
    k.paint(m, color, on=list(on))


# --- the eye: big and round, pink iris in white, black pupil, a glint -------------------------------------------------
def eye_colour(white=WHITE, iris=PINK, iris_dark=PINKDARK, pupil=INK, iris_r=0.8, pupil_w=0.3, pupil_h=0.46,
               cut=None, cut_col=None):
    CW, CI, CD, CP = srgb(white), srgb(iris), srgb(iris_dark), srgb(pupil)

    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        r = np.hypot(u + 0.02, v + 0.02)
        t = np.clip((v + 0.3) / 0.9, 0, 1)[:, None]
        c[r < iris_r] = (CI[None] * (1 - t) + CD[None] * t)[r < iris_r]
        c[np.hypot((u + 0.02) / pupil_w, (v + 0.02) / pupil_h) < 1] = CP
        c[np.hypot((u - 0.15) / 0.15, (v - 0.22) / 0.17) < 1] = CW
        if cut is not None:
            c[v > cut(u)] = srgb(cut_col)
        return c
    return eye


def lay_eyes(k, state, at, n, half, open_look, lid, ink, outline=None, eps=0.0018, line=0.16):
    """Two patches laid by hand, u outward on both (the left one's colour function and outline flipped)."""
    look = eye_look(state, open_look, srgb(lid), srgb(ink), line=line)
    at, n = np.array(at, float), np.array(n, float)
    k.decal(at, n, half, look, eps=eps, outline=polygon(outline) if outline else None, rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(at * q, n * q, half, lambda u, v: look(-u, v), eps=eps,
            outline=polygon([(-a, b) for a, b in outline]) if outline else None, rings=20, segs=56)


# --- Sprigatito's own ---------------------------------------------------------------------------------------------------
MASK = [(0.0, 0.03), (0.022, 0.058), (0.05, 0.066), (0.095, 0.098), (0.118, 0.032), (0.112, -0.022), (0.084, -0.046),
        (0.046, -0.046), (0.016, -0.032), (0.0, -0.036)]
EYE_X, EYE_Z = 0.05, 0.0
EYE_HALF = (0.03, 0.032)


def add_head(k):
    k.add("head", ell(H, HR, deform=boxy(2.15)), PALE, "head")
    k.add("muzzle", ell(H + np.array([0, -0.066, -0.05]), (0.05, 0.036, 0.036)), PALE, "head")
    # cheeks filling out low on the face, where the tufts spring from
    k.add("cheek", ell(H + np.array([0.08, -0.026, -0.03]), (0.062, 0.054, 0.056)), PALE, "head", mirror=True)


def add_ears(k):
    for s, nm, b in ((1, "earR", "earR"), (-1, "earL", "earL")):
        m = np.array([s, 1, 1.0])
        root = np.array([0.072, -0.052, 0.358]) * m
        tip = np.array([0.135, -0.05, 0.452]) * m
        ear(k, root, tip, 0.054, 0.3, BRIGHT, LINING, b, nm)


def add_tufts(k):
    """Great pale tufts flaring out from the cheeks: up and out, out, down and out."""
    # broad wisps: thin front to back, deep up and down, so their faces show to the front
    T = [((0.75, 0.1, 0.8), 0.068, 0.04), ((1.0, 0.14, 0.15), 0.078, 0.044), ((0.85, 0.12, -0.55), 0.05, 0.034)]
    for i, (dd, ln, w) in enumerate(T):
        for s, side in ((1, "R"), (-1, "L")):
            root = H + np.array([s * 0.112, -0.02, -0.03 + 0.04 * dd[2]])
            d = (s * dd[0], dd[1], dd[2])
            k.add(f"tuft{i}{side}", spike(root, d, ln, 0.012, w), PALE, "head", fuse=False, surface="fur")


def add_face(k):
    mask_paint(k, mirrored(MASK), BRIGHT)
    # the nose: a small pink triangle at the mask's point
    p, n = on_head(0.0, H[2] - 0.03, H, HR)
    k.decal(p + np.array([0, -0.014, 0]), unit(0, -1, 0.05), (0.011, 0.008), NOSE, eps=0.0016,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    # the open mouth: a lavender opening with a dark rim, two little fangs at the top corners
    CM, CR, CW = srgb(MOUTH), srgb(MOUTHRIM), srgb(WHITE)
    shape = [(-0.9, 0.7), (0.9, 0.7)] + [(math.cos(a), 0.15 + 0.85 * math.sin(a)) for a in np.linspace(-0.15, -math.pi + 0.15, 11)]
    rim = polygon(shape)

    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        rr = np.hypot(u, v) / np.array([rim(t) for t in np.arctan2(v, u)])
        c[rr > 0.9] = CR
        for x0 in (-0.48, 0.48):
            c[(v > 0.32) & (v < 0.75) & (np.abs(u - x0) < 0.2 * (v - 0.32) / 0.43)] = CW
        return c
    k.decal(H + np.array([0, -0.107, -0.062]), unit(0, -1, -0.15), (0.021, 0.018), mouth, eps=0.0018,
            outline=rim, rings=12, segs=56)


def add_body(k):
    k.add("chest", ell((0, -0.045, 0.15), (0.056, 0.05, 0.058)), PALE, "body")
    k.add("middle", ell((0, 0.015, 0.132), (0.055, 0.07, 0.052)), PALE, "body")
    k.add("rump", ell((0, 0.062, 0.135), (0.056, 0.045, 0.055)), PALE, "body")


def add_legs(k):
    for bone, y, nm in (("leg{s}", -0.05, "fore"), ("hind{s}", 0.068, "hind")):
        k.add(f"{nm}top", ell((0.042, y + 0.002, 0.11), (0.034, 0.038, 0.048)), PALE, bone, mirror=True)
        k.add(f"{nm}leg", tube([(0.045, y, 0.1), (0.046, y - 0.002, 0.055), (0.047, y - 0.004, 0.022)],
                               [0.023, 0.021, 0.02]), PALE, bone, mirror=True)
        k.add(f"{nm}paw", ell((0.047, y - 0.01, 0.013), (0.023, 0.028, 0.0135)), PALE, bone, mirror=True)


TAIL = [(0.08, 0.16), (0.12, 0.185), (0.155, 0.225), (0.17, 0.272), (0.162, 0.312), (0.14, 0.33)]
TAIL_R = [0.02, 0.027, 0.034, 0.037, 0.033, 0.024]


def tail_pts(tail=TAIL, out=TAIL_OUT):
    y0 = tail[0][0]
    return [np.array([0, y0, z]) + out * (y - y0) for y, z in tail]


def add_tail(k):
    pts = tail_pts()
    k.add("tail", tube(pts, TAIL_R), PALE, "tail")
    end = pts[-1]
    d = unit(*(pts[-1] - pts[-2]))
    for i, (dd, ln) in enumerate([((0, 0, 0), 0.05), ((0.5, 0, 0.6), 0.035), ((-0.4, 0.3, 0.5), 0.03)]):
        dir_ = unit(*(d + np.array(dd)))
        k.add(f"tailtuft{i}", spike(end - d * 0.006, dir_, ln, 0.018, 0.013), GREY, "tail", fuse=False, surface="fur")
    # the tail greyer toward its end
    k.paint(lambda P: smoothstep(0.265, 0.32, P[:, 2]) * (P[:, 1] > 0.06), GREY, on=["tail"])


def add_collar(k):
    """Bright green leaves round the throat, pointing out and down, a pale leaf in the middle."""
    c = np.array([0.0, -0.088, 0.168])
    for i, (dx, dz, ln, w) in enumerate([(0.55, -0.55, 0.062, 0.024), (0.95, -0.12, 0.06, 0.022),
                                          (0.25, -0.95, 0.062, 0.024)]):
        for s, side in ((1, "R"), (-1, "L")):
            d = unit(s * dx, -0.35, dz)
            flat_leaf(k, f"collar{i}{side}", c + np.array([s * 0.012, 0, 0]), d, ln, w, BRIGHT, "body", 0.012)
    flat_leaf(k, "collarmid", c + np.array([0, -0.006, 0.004]), unit(0, -0.45, -1), 0.05, 0.018, LINING, "body", 0.013)
    k.add("collarcore", outward(ell(c + np.array([0, 0.004, 0.0]), (0.03, 0.02, 0.014))), LEAFDARK, "body",
          fuse=False, surface="leaf")


def sprigatito(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k)
    add_tufts(k)
    add_tail(k)
    add_collar(k)
    add_face(k)
    k.build(voxel=0.004, smooth=4, band=0.004, wband=0.04)
    at, n = on_head(EYE_X, H[2] + EYE_Z, H, HR)
    n = unit(n[0] * 1.15, n[1], n[2])
    k.eye_states(lambda s: lay_eyes(k, s, at, n, EYE_HALF, eye_colour(), BRIGHT, INK))
    k.export(out_path)


if __name__ == "__main__":
    sprigatito(*args())
