# Litten sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/litten.py -- <the export's Litten.glb> <out.glb>
# From the references: a slim dark slate-black kitten with a very big round head; large pointed ears standing up from
# the head's upper corners, lined pale grey; on the forehead a red mark of three bars (a short crossbar high up, a bar
# down the middle and a broad bar across just over the eyes); big yellow eyes, sly, their tops cut straight by a dark
# lid that slopes down to the nose, a red slit of a pupil; a red lower face, a small dark nose and a little mouth; red
# tufts of fur in three points out from each cheek (the face's widest part); a few small points of fur on the crown;
# thin dark legs, each with two red bands near the foot; a long thin tail rising in an S behind, ending in a spiky
# tuft. (The pictures overrule the task's text: the lower face and the cheek tufts are red as well as the legs'
# stripes and the forehead's mark; the eyes' pupils are red slits.)
# Reference ratios (3D picture): head without ears / height to the ear tips 0.64 (it crouches; standing here 0.44 of
# the height), head width / body width about 2.4 (here 2.0), tufts' span / head width 1.47 (here 1.5), leg showing under
# the belly / height 0.34 (here 0.25, belly lowered against stilts), tail tip a quarter
# above the ear tips (kept at the ear tips here, so the body is not drawn small).
#
# Written to be grown from (torracat.py and incineroar.py keep this frame: the head and its front coordinates, the
# ears and their lining, the sly cut eye, the cheek tufts, the bands round a limb, the tail's spiky tuft). Every part
# is a function taking the kit and its sizes; the calls are at the end under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, slab, ring_band, outward, srgb, inside, smoothstep, unit, eye_look,
                       polygon, axes, args)

# --- colours ------------------------------------------------------------------------------------------------------------
DARK = "#31353F"          # the coat: a dark slate with a little blue
RED = "#EC4A3A"
EARIN = "#AEB3C0"
YELLOW, AMBER = "#F8D42C", "#F09A1E"
PUPIL = "#7A1018"
INK = "#121318"
NOSE = "#3A2A2E"
WHITE = "#FFFFFF"

# --- the frame (the skeleton's joints: body (0, 0, 0.1), head (0, -0.05, 0.14), legs (+-0.03, -0.05, 0.08), hind legs
#     (+-0.03, 0.05, 0.08), tail (0, 0.06, 0.11)) ----------------------------------------------------------------------
H = np.array([0.0, -0.086, 0.163])       # the head's middle, up and forward of its joint
HR = np.array([0.066, 0.056, 0.057])     # a big round head, a little wider than tall
LEG_X = 0.03
FORE_Y, HIND_Y = -0.05, 0.052


# --- shared helpers: the head's front, coordinates round it, a soft polygon ---------------------------------------------
def on_head(x, z, h, hr, lift=0.0):
    """The head's front point at (x, z) on its ellipsoid, lifted along its normal, and the normal."""
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / np.array(hr) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def round_head(P, h, hr):
    """Coordinates round the head's upright axis: the signed arc out from the face's middle (metres) and height."""
    return np.arctan2(P[:, 0], -(P[:, 1] - h[1])) * hr[0], P[:, 2]


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


# --- the head: big and round, a small muzzle low on the face ----------------------------------------------------------
def add_head(k, h=H, hr=HR, color=DARK, bone="head", muzzle=True):
    k.add("head", ell(h, hr), color, bone)
    if muzzle:
        k.add("muzzle", ell(h + np.array(hr) * np.array([0, -0.74, -0.5]), np.array(hr) * np.array([0.44, 0.36, 0.34])),
              color, bone)


# --- the ears: large, pointed, from the head's upper corners, turned a little out; a pale lining on their fronts -----
def add_ears(k, h=H, hr=HR, color=DARK, lining=EARIN, at=(0.62, -0.1, 0.62), tip=(0.05, 0.02, 0.06), base=0.026,
             depth=0.3, lining_share=0.62, bone="head", lining_len=1.0):
    """at: the ear's root as shares of the head's radii; tip: the tip's offset from the root (metres, +X side)."""
    hr = np.array(hr)
    dp = base * depth

    def prof(t):
        """The ear's half width and half depth at a share t of its length (a kink out at 0.45, a point at the tip)."""
        if t <= 0.45:
            f = t / 0.45
            return base * (1 - 0.4 * f), dp * (1 - 0.2 * f)
        f = (t - 0.45) / 0.55
        return base * 0.6 * (1 - f), dp * 0.8 * (1 - f)

    for s in (1, -1):
        m = np.array([s, 1, 1.0])
        root = (h + hr * np.array(at)) * m          # the head's middle is on the midline: only x turns
        tp = root + np.array(tip) * m
        d = tp - root
        L = np.linalg.norm(d); d = d / L
        across = unit(*np.cross(d, (0, 1.0, 0)))
        face = unit(0, -1, 0.15)
        face = unit(*(face - d * (face @ d)))
        # a fat triangle of a solid: the root's corners round the head, a point at the tip
        pts = []
        for t in (0.0, 0.45):
            w, f = prof(t)
            c = root + d * L * t
            pts += [c + across * w + face * f, c - across * w + face * f, c + across * w - face * f,
                    c - across * w - face * f]
        pts.append(tp)
        k.add("ear" + ("R" if s > 0 else "L"), hull(pts), color, bone, fuse=False, surface="fur")
        # the lining: a thin solid just proud of the ear's front face, a smaller triangle following its slope
        lp = []
        for t in (0.1 * lining_len, 0.45 * lining_len, 0.8 * lining_len):
            w, f = prof(t)
            w *= lining_share * (1 if t < 0.7 * lining_len else 0.5)
            c = root + d * L * t
            for off in (f + dp * 0.12, f - dp * 0.35):
                lp += [c + across * w + face * off, c - across * w + face * off]
        lp.append(root + d * L * 0.9 * lining_len + face * (prof(0.9 * lining_len)[1] + dp * 0.08))
        k.add("earin" + ("R" if s > 0 else "L"), hull(lp), lining, bone, fuse=False, surface="fur")


# --- the cheek tufts: three solid points out from each cheek, the face's widest part ------------------------------------
TUFTS = [((0.95, -0.15, 0.1), 0.028, 0.019), ((1.0, -0.1, -0.3), 0.034, 0.021), ((0.85, -0.22, -0.75), 0.026, 0.017)]


def add_tufts(k, h=H, hr=HR, color=RED, tufts=TUFTS, size=1.0, back=0.0, bone="head", name="tuft"):
    hr = np.array(hr)
    for i, ((ax, ay, az), ln, w) in enumerate(tufts):
        for s in (1, -1):
            root = h + np.array([s * hr[0] * 0.8, -hr[1] * 0.35 + back, hr[2] * az * 0.75])
            d = (s * ax, ay, az * 0.45)
            k.add(f"{name}{i}{'R' if s > 0 else 'L'}", spike(root, d, ln * size + hr[0] * 0.2, w * size, w * size * 0.55),
                  color, bone, fuse=False, surface="fur")


# --- the red lower face: from under the eyes, out to the cheeks, round the muzzle and the chin -------------------------
FACE = [(0.0, -0.026), (0.016, -0.025), (0.034, -0.026), (0.05, -0.02), (0.064, -0.008), (0.078, -0.012),
        (0.08, -0.04), (0.06, -0.058), (0.03, -0.066), (0.0, -0.068)]


def add_lower_face(k, h=H, hr=HR, outline=FACE, color=RED, on=("head", "muzzle")):
    poly = mirrored(outline)
    def mask(P):
        s, z = round_head(P, h, hr)
        front = (P[:, 1] < h[1] - hr[1] * 0.15)
        return poly_mask(s, z - h[2], poly, 0.0012) * front
    k.paint(mask, color, on=list(on))


# --- the forehead's mark: three red bars, each its own patch (a crossbar, a bar down the middle, a bar over the eyes) -
MARK = [  # (x, z from the head's middle, half sizes, tilt in degrees)
    (0.0, 0.03, (0.0072, 0.021), 0),
    (0.0, 0.035, (0.025, 0.0055), 0),
    (0.0, 0.01, (0.038, 0.0058), 0),
]


def add_mark(k, h=H, hr=HR, mark=MARK, color=RED, eps=0.0018):
    rect = polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
    for x, dz, half, tilt in mark:
        p, n = on_head(x, h[2] + dz, h, hr)
        up = unit(math.sin(math.radians(tilt)), 0, math.cos(math.radians(tilt)))
        k.decal(p, n, half, color, up=up, eps=eps, outline=rect, rings=8, segs=40)


# --- the nose and the little mouth --------------------------------------------------------------------------------------
def add_nose_mouth(k, at, half, ground=RED, nose=NOSE, ink=INK, eps=0.0016):
    CG, CI = srgb(ground), srgb(ink)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.25 + 1.6 * (np.abs(u) - 0.45) ** 2)) < 0.2) & (np.abs(u) < 0.9) & (v < 0.25)] = CI
        return c
    k.decal(at, unit(0, -1, -0.2), half, mouth, eps=eps, rings=10, segs=48)
    k.decal(at + np.array([0, -0.001, half[1] * 1.25]), unit(0, -1, 0.15), (half[0] * 0.42, half[1] * 0.5), nose,
            eps=eps * 1.4, outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)


# --- the eyes: big, yellow going amber at the bottom, the top cut straight by a dark lid sloping down to the nose, a
#     red slit of a pupil, a glint. Two patches laid by hand, u outward on both -------------------------------------
OUTLINE = [(-1.0, 0.16), (-0.5, 0.36), (0.2, 0.54), (0.86, 0.66), (1.0, 0.32), (0.88, -0.32), (0.45, -0.74),
           (-0.15, -0.84), (-0.68, -0.6), (-0.95, -0.22)]


def eye_colour(yellow=YELLOW, amber=AMBER, pupil=PUPIL, ink=INK, outline=OUTLINE, pupil_w=0.2, pupil_h=0.64,
               lid=0.62):
    CY, CA, CP, CI = srgb(yellow), srgb(amber), srgb(pupil), srgb(ink)
    rim = polygon(outline)
    def eye(u, v):
        t = np.clip((0.1 - v) / 0.9, 0, 1)[:, None]
        c = CY[None] * (1 - t) + CA[None] * t
        c[np.hypot((u + 0.04) / pupil_w, (v + 0.06) / pupil_h) < 1] = CP
        c[np.hypot((u - 0.3) / 0.13, (v + 0.12) / 0.15) < 1] = srgb(WHITE)
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[(rr > lid) & (v > 0.05 + 0.3 * u)] = CI           # the dark lid along the top edge
        c[rr > 0.82] = CI
        return c
    return eye


def lay_eyes(k, state, h=H, hr=HR, x=0.034, dz=-0.011, half=(0.022, 0.018), lid=DARK, line=RED, outline=OUTLINE,
             look=None, tilt=1.15, eps=0.002):
    p, n = on_head(x, h[2] + dz, h, hr)
    n = unit(n[0] * tilt, n[1], n[2])
    CL, CI = srgb(lid), srgb(line)
    open_look = look or eye_colour(outline=outline)
    look = eye_look(state, open_look, CL, CI, line=0.17)
    k.decal(p, n, half, look, eps=eps, outline=polygon(outline), rings=20, segs=56)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, half, lambda u, v: look(-u, v), eps=eps, outline=polygon([(-a, b) for a, b in outline]),
            rings=20, segs=56)


# --- bands round a limb: loose rings just over its surface, seam behind -------------------------------------------------
def add_bands(k, name, a, b, r, shares, length, color=RED, bone="leg{s}", mirror=True, thick=0.0024):
    """Bands round the straight stretch from a to b of radius r (at a and at b, a pair), at shares along it."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a
    for i, t in enumerate(shares):
        c = a + d * t
        rr = r[0] + (r[1] - r[0]) * t
        k.add(f"{name}{i}", ring_band(c, d, rr - thick * 0.3, length, thick=thick), color, bone, fuse=False,
              mirror=mirror, surface="fur")


# --- the tail's spiky tuft: solid points out of the end ----------------------------------------------------------------
def add_tail_tuft(k, end, d, size, color=DARK, n=5, bone="tail", name="tailspike"):
    end = np.array(end, float)
    d = unit(*d)
    a, b = axes(d)
    k.add(name + "ball", outward(ell(end, (size * 0.42,) * 3, sub=3)), color, bone, fuse=False, surface="fur")
    for i in range(n):
        ang = 2 * math.pi * i / n + 0.3
        out = d * 1.0 + (a * math.cos(ang) + b * math.sin(ang)) * (0.9 if i else 0.0)
        out = unit(*out)
        k.add(f"{name}{i}", spike(end + out * size * 0.2, out, size * (1.25 if i == 0 else 0.95), size * 0.3, size * 0.3),
              color, bone, fuse=False, surface="fur")


# --- Litten's own: a slim body, thin banded legs, the tail, the crown's points ----------------------------------------
TAIL_OUT = unit(0.57, 0.82, 0)     # the tail's sweep: back and some 35 degrees out to +X


def add_body(k):
    k.add("chest", ell((0, -0.032, 0.104), (0.036, 0.04, 0.039)), DARK, "body")
    k.add("middle", ell((0, 0.012, 0.098), (0.033, 0.046, 0.034)), DARK, "body")
    k.add("rump", ell((0, 0.05, 0.1), (0.036, 0.034, 0.038)), DARK, "body")
    k.add("neck", ell((0, -0.06, 0.13), (0.032, 0.03, 0.036)), DARK, "head")


LEG_R = (0.0125, 0.0112)


def add_legs(k):
    for bone, y, nm in (("leg{s}", FORE_Y, "fore"), ("hind{s}", HIND_Y, "hind")):
        k.add(f"{nm}top", ell((LEG_X - 0.003, y + 0.002, 0.082), (0.02, 0.024, 0.032)), DARK, bone, mirror=True)
        k.add(f"{nm}leg", tube([(LEG_X, y, 0.08), (LEG_X, y - 0.002, 0.045), (LEG_X, y - 0.003, 0.016)],
                               [LEG_R[0], LEG_R[0] * 0.95, LEG_R[1]]), DARK, bone, mirror=True)
        k.add(f"{nm}paw", ell((LEG_X + 0.001, y - 0.009, 0.0095), (0.0135, 0.017, 0.0095)), DARK, bone, mirror=True)
        add_bands(k, f"{nm}band", (LEG_X, y - 0.002, 0.045), (LEG_X, y - 0.003, 0.016), (LEG_R[0] * 0.95, LEG_R[1]),
                  (0.2, 0.72), 0.0085, bone=bone)


TAIL = [(0.0, 0.078, 0.11), (0.0, 0.1, 0.12), (0.0, 0.122, 0.15), (0.0, 0.128, 0.185), (0.0, 0.12, 0.212),
        (0.0, 0.108, 0.228)]


def tail_pts(tail=TAIL, out=TAIL_OUT):
    """The tail's line: authored in the midline plane (y back), turned to sweep out toward +X."""
    pts = []
    for x, y, z in tail:
        back = y - tail[0][1]
        pts.append(np.array([0, tail[0][1], z]) + out * back)
    return pts


def add_tail(k):
    pts = tail_pts()
    k.add("tail", tube(pts, [0.0075, 0.0068, 0.0064, 0.0062, 0.006, 0.0056]), DARK, "tail")
    d = pts[-1] - pts[-2]
    add_tail_tuft(k, pts[-1] + unit(*d) * 0.004, d, 0.016)


def add_crown(k, h=H, hr=HR):
    for i, (x, ln, lean) in enumerate(((0.0, 0.016, 0.0), (0.016, 0.012, 0.4), (-0.016, 0.012, -0.4))):
        b = h + np.array([x, 0.004, hr[2] * 0.93])
        k.add(f"crown{i}", spike(b, (lean, 0.25, 1.0), ln, 0.0065), DARK, "head", fuse=False, surface="fur")


def litten(src, out_path):
    k = Kit(src, coat="fur")
    add_body(k)
    add_legs(k)
    add_head(k)
    add_ears(k, tip=(0.03, 0.008, 0.062), base=0.025)
    add_crown(k)
    add_tufts(k)
    add_tail(k)
    add_lower_face(k)
    add_mark(k)
    add_nose_mouth(k, H + np.array([0, -HR[1] * 1.08, -HR[2] * 0.6]), (0.0085, 0.006))
    k.build(voxel=0.0028, smooth=4, band=0.003, wband=0.025)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    litten(*args())
