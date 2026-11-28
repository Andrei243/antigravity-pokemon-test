# Pisicut sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pisicut.py -- <the export's Pisicut.glb> <out.glb>
# From the picture (10031-Pisicut.jpg, the project's own): a small round grey tabby kitten lying as a loaf, its paws
# tucked in under its chest: a great rounded body whose back rises higher than the head, a big round head fused low on
# its front, fat cheeks, the lower face, the chest and the two tucked front paws white; small rounded ears with pink
# insides; sleepy half-shut green eyes under short dark brows, a small pink nose and a tiny mouth; soft dark grey tabby
# stripes tapering down the flanks, three on the forehead; a striped tail curled round its side; a red woollen thread
# round its neck tied in a little bow on its chest.
#
# Written to be grown from (motanor.py imports it with sibling("pisicut")): the line's colours, the eyes, the brows,
# the nose and mouth, the ears, the stripes, the thread and the tail are functions; Pisicut's calls are under __main__.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, ring_band, outward, srgb, inside, unit, eye_look, polygon, axes, args,
                       sibling)

L = sibling("litten")          # on_head, round_head, poly_mask, mirrored, add_ears

# --- the line's colours ----------------------------------------------------------------------------------------------
GREY = "#B9B7BB"
STRIPE = "#85838B"
PALE = "#F1EFEB"
EARIN = "#E2A6AE"
NOSE = "#E59AA6"
THREAD = "#B9413D"
IRIS, IRIS_LO = "#7E9A52", "#4E6A34"
PUPIL = "#1E2418"
SCLERA = "#E9EBDD"
INK = "#2A2629"
WHITE = "#FFFFFF"

# --- Pisicut's frame (joints: body (0, 0.03, 0.137), head (0, -0.11, 0.177), ears (+-0.07, -0.12, 0.287), legs
#     (+-0.05, -0.13, 0.067), hind (+-0.12, 0.1, 0.087), tail (0, 0.2, 0.057)) --------------------------------------
H = np.array([0.0, -0.157, 0.19])         # the head's middle, low on the body's front
HR = np.array([0.13, 0.108, 0.104])       # a tenth over the picture's measure


# --- eyes: sleepy, half shut: the upper half of the almond the grey lid, a dark lid line across, a green iris with a
#     dark pupil below it, pale round it, a thin dark lower rim ----------------------------------------------------------
EYE_OUTLINE = [(-1.0, 0.12), (-0.62, 0.5), (0.0, 0.6), (0.62, 0.54), (1.0, 0.24), (0.82, -0.3), (0.35, -0.66),
               (-0.3, -0.66), (-0.8, -0.34)]


def eye_colour(lid=GREY, iris=IRIS, iris_lo=IRIS_LO, pupil=PUPIL, sclera=SCLERA, ink=INK, lidline=0.12,
               pupil_w=0.17, glint=True):
    CL, CI, CIL, CP, CS, CK = srgb(lid), srgb(iris), srgb(iris_lo), srgb(pupil), srgb(sclera), srgb(ink)
    rim = polygon(EYE_OUTLINE)
    def eye(u, v):
        line = lidline - 0.12 * u                                # the lid droops a little to the outer corner
        c = np.tile(CS, (len(u), 1))
        r_ir = np.hypot(u / 0.52, (v + 0.12) / 0.62)
        t = np.clip((0.1 - v) / 0.7, 0, 1)[:, None]
        ir = CI[None] * (1 - t) + CIL[None] * t
        sel = r_ir < 1
        c[sel] = ir[sel]
        c[np.hypot(u / pupil_w, (v + 0.12) / 0.42) < 1] = CP
        if glint:
            c[np.hypot((u + 0.18) / 0.12, (v + 0.05) / 0.12) < 1] = srgb(WHITE)
        rr = np.hypot(u, v) / np.array([rim(a) for a in np.arctan2(v, u)])
        c[(rr > 0.84) & (v < line)] = CK                         # the thin dark rim below
        c[v > line] = CL                                         # the heavy lid over the top half
        c[np.abs(v - line) < 0.14] = CK                          # its dark edge
        return c
    return eye


def lay_eyes(k, state, h=H, hr=HR, x=0.061, dz=-0.006, half=(0.034, 0.025), lid=GREY, line=INK, look=None,
             tilt=1.1, eps=0.002):
    p, n = L.on_head(x, h[2] + dz, h, hr)
    n = unit(n[0] * tilt, n[1], n[2])
    CL, CI = srgb(lid), srgb(line)
    open_look = look or eye_colour(lid=lid)
    look = eye_look(state, open_look, CL, CI, line=0.17)
    up = unit(-0.12, 0, 1)                                      # the eye's slant: its outer corner a little low
    k.decal(p, n, half, look, eps=eps, outline=polygon(EYE_OUTLINE), rings=20, segs=56, up=up)
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, half, lambda u, v: look(-u, v), eps=eps,
            outline=polygon([(-a, b) for a, b in EYE_OUTLINE]), rings=20, segs=56, up=up * q)


def add_brows(k, h=H, hr=HR, x=0.062, dz=0.035, half=(0.013, 0.0045), color=STRIPE, tilt=-14):
    """A short soft dark stroke over each eye, its inner end a little higher."""
    for s in (1, -1):
        p, n = L.on_head(x * s, h[2] + dz, h, hr)
        a = math.radians(tilt * s)
        k.decal(p, n, half, color, up=unit(math.sin(a), 0, math.cos(a)), eps=0.0018, rings=8, segs=36)


def add_nose_mouth(k, h=H, hr=HR, dz=-0.03, nose=NOSE, ink=INK, ground=PALE, size=1.0, eps=0.0016):
    """A small pink nose on the muzzle, a tiny mouth under it: a short stroke down and a little bow."""
    p, n = L.on_head(0.0, h[2] + dz, h, hr)
    p = p + np.array([0, -0.012 * size, 0])
    k.decal(p, unit(0, -1, 0.25), (0.012 * size, 0.009 * size), nose, eps=eps,
            outline=polygon([(-1, 0.7), (1, 0.7), (0, -1)]), rings=6, segs=24)
    CG, CI = srgb(ground), srgb(ink)
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(u) < 0.11) & (v > -0.1) & (v < 0.75)] = CI
        c[(np.abs(v - (-0.1 - 0.55 * np.abs(u) + 0.35 * u * u)) < 0.17) & (np.abs(u) < 0.8)] = CI
        return c
    k.decal(p + np.array([0, 0.002, -0.022 * size]), unit(0, -1, -0.1), (0.016 * size, 0.012 * size), mouth,
            eps=eps, rings=12, segs=48)


# --- tabby stripes: soft dark bands over the back, tapering down the flanks --------------------------------------------
def stripes(k, on, y0, y1, period, top, bottom, slant=0.3, width=0.2, front=None, color=STRIPE):
    def mask(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        s = (y + slant * (top - z)) / period
        f = np.abs(s - np.round(s))
        h = np.clip((z - bottom) / (top - bottom), 0, 1)
        w = width * (0.25 + 0.75 * h) * (1 + 0.15 * np.sin(x * 40))
        m = inside(f - w, 0.012) * (z > bottom) * (y > y0) * (y < y1)
        if front is not None:
            m = m * front(P)
        return m
    k.paint(mask, color, on=on)


def rump_stripes(k, on, y0, top, bottom, period, width=0.17, color=STRIPE):
    """Over the rump, seen from behind: arches across it whose ends run down the haunches (the back's stripes carried
    round)."""
    def mask(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        s = (z + 0.55 * x) / period
        f = np.abs(s - np.round(s))
        h = np.clip((z - bottom) / (top - bottom), 0, 1)
        w = width * (0.35 + 0.65 * h)
        return inside(f - w, 0.012) * (z > bottom) * smooth_from(y, y0)
    k.paint(mask, color, on=on)


def smooth_from(y, y0, edge=0.004):
    return inside(y0 - y, edge)


def forehead(k, h=H, hr=HR, color=STRIPE, xs=(-0.03, 0.0, 0.03), z0=None, w=0.0075):
    z0 = h[2] + hr[2] * 0.5 if z0 is None else z0
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        m = np.zeros(len(P))
        for xi in xs:
            ww = w * np.clip((z - z0) / 0.03, 0.3, 1.0)
            m = np.maximum(m, inside(np.abs(x - xi * (1 + 0.6 * (z - z0) / 0.05)) - ww, 0.002))
        return m * (z > z0) * (y < h[1] + hr[1] * 0.3)
    k.paint(mask, color, on=["head"])


def cheek_stripes(k, h=H, hr=HR, color=STRIPE):
    """Two short streaks back from the outer corner of each eye along the side of the head."""
    def mask(P):
        x, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
        m = np.zeros(len(P))
        for dz, x0 in ((0.008, 0.1), (-0.018, 0.105)):
            zc = h[2] + dz - 0.25 * (x - x0)
            m = np.maximum(m, inside(np.abs(z - zc) - 0.0055, 0.002) * (x > x0) * (x < x0 + 0.045))
        return m * (y < h[1] + 0.04)
    k.paint(mask, color, on=["head"])


# --- the red woollen thread round the neck and its bow ----------------------------------------------------------------
def ring_pts(cy, rx, ry, z0, dz, n=40):
    pts = []
    for i in range(n + 1):
        a = math.pi + 0.08 + (2 * math.pi - 0.16) * i / n       # the seam at the back, hidden in the fur
        pts.append((rx * math.sin(a), cy - ry * math.cos(a), z0 + dz * math.cos(a)))
    return pts


def add_thread(k, cy, rx, ry, z0, dz, r, bow=True, scale=1.0, frayed=False, color=THREAD):
    k.add("thread", outward(tube(ring_pts(cy, rx, ry, z0, dz), [r] * 41, seg=10, per=3)), color, "body", fuse=False,
          surface="cloth")
    knot = np.array([0, cy - ry - r * 0.6, z0 + dz])
    s = scale
    k.add("knot", outward(ell(knot, (r * 1.9, r * 1.7, r * 1.9), sub=3)), color, "body", fuse=False, surface="cloth")
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        if bow:
            loop = [knot + np.array(d) * m * s for d in
                    ((0.008, -0.002, 0.004), (0.022, -0.004, 0.016), (0.04, -0.004, 0.017), (0.045, -0.002, 0.002),
                     (0.03, -0.001, -0.009), (0.01, 0.0, -0.003))]
            k.add(f"bowloop{'R' if side > 0 else 'L'}", outward(tube(loop, [r * 0.9] * len(loop), seg=10, per=4)),
                  color, "body", fuse=False, surface="cloth")
        ends = ((0.006, -0.002, -0.012), (0.014, -0.004, -0.03), (0.02, -0.004, -0.045)) if not frayed else \
            ((0.006, -0.004, -0.015), (0.012, -0.01, -0.04), (0.016, -0.012, -0.065))
        tail = [knot] + [knot + np.array(d) * m * s for d in ends]
        k.add(f"bowend{'R' if side > 0 else 'L'}", outward(tube(tail, [r * 0.9, r * 0.85, r * 0.8, r * 0.7], seg=10,
                                                                 per=4)), color, "body", fuse=False, surface="cloth")


# --- a striped tail: a loose tube with dark rings round it ------------------------------------------------------------
def add_tail(k, pts, radii, shares, band_len, color=GREY, band=STRIPE, bone="tail"):
    k.add("tail", outward(tube(pts, radii, seg=18, per=8)), color, bone, fuse=False, surface="fur")
    P = np.array(pts, float)
    seg = np.linalg.norm(np.diff(P, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)]) / seg.sum()
    for i, t in enumerate(shares):
        j = min(int(np.searchsorted(cum, t)) - 1, len(P) - 2)
        j = max(j, 0)
        f = (t - cum[j]) / (cum[j + 1] - cum[j])
        c = P[j] + (P[j + 1] - P[j]) * f
        rr = radii[j] + (radii[j + 1] - radii[j]) * f
        k.add(f"tailband{i}", ring_band(c, P[j + 1] - P[j], rr + 0.0009, band_len, thick=0.004), band, bone,
              fuse=False, surface="fur")


# --- Pisicut ----------------------------------------------------------------------------------------------------------
TAIL = [(0.02, 0.235, 0.045), (0.1, 0.25, 0.04), (0.175, 0.19, 0.036), (0.205, 0.09, 0.034), (0.2, 0.0, 0.032),
        (0.17, -0.06, 0.031)]


def pisicut(src, out_path):
    k = Kit(src, coat="fur")
    # The loaf: a great rounded body, its back higher than the head, a wide flat bottom
    k.add("loaf", ell((0, 0.03, 0.135), (0.168, 0.215, 0.135)), GREY, "body")
    k.add("rump", ell((0, 0.1, 0.15), (0.158, 0.155, 0.14)), GREY, "body")
    k.add("base", ell((0, 0.03, 0.06), (0.172, 0.22, 0.06)), GREY, "body")
    k.add("hind", ell((0.13, 0.12, 0.075), (0.058, 0.095, 0.07)), GREY, "hind{s}", mirror=True)
    k.add("chest", ell((0, -0.125, 0.105), (0.13, 0.1, 0.1)), GREY, "body")
    # The head, big and round, fused low on the front, fat cheeks and a small muzzle
    k.add("head", ell(H, HR), GREY, "head")
    k.add("cheek", ell((0.078, -0.19, 0.138), (0.062, 0.058, 0.05)), PALE, "head", mirror=True)
    k.add("muzzle", ell((0, -0.245, 0.155), (0.042, 0.028, 0.03)), PALE, "head")
    # The front paws tucked in under the chest, white
    k.add("arm", tube([(0.065, -0.14, 0.07), (0.058, -0.2, 0.045), (0.05, -0.245, 0.035)], [0.04, 0.036, 0.032]), GREY,
          "leg{s}", mirror=True)
    k.add("paw", ell((0.046, -0.262, 0.03), (0.045, 0.046, 0.03)), PALE, "leg{s}", mirror=True)

    # The white lower face, chest and bib, paws white to the wrist
    def lower_face(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        top = 0.168 + 0.016 * inside(np.abs(x) - 0.022, 0.006)
        return inside(z - top, 0.0025) * inside(y - (H[1] - 0.03), 0.004) * (z > 0.07)
    k.paint(lower_face, PALE, on=["head", "cheek", "muzzle"])
    k.paint(lambda P: inside(np.hypot(P[:, 0] / 0.085, (P[:, 2] - 0.085) / 0.07) - 1, 0.003)
            * inside(P[:, 1] + 0.17, 0.004), PALE, on=["chest", "head", "cheek", "loaf", "arm"])
    k.paint(lambda P: inside(P[:, 1] + 0.23, 0.004) * (P[:, 2] < 0.06), PALE, on=["arm", "paw"])
    # A pale strip low on the flanks, as under the picture's tail
    k.paint(lambda P: inside(P[:, 2] - 0.022, 0.004), PALE, on=["base"])

    # Stripes over the back and down the flanks, three on the forehead, two streaks by each eye
    stripes(k, ["loaf", "rump", "base", "hind", "chest"], -0.11, 0.215, 0.058, 0.29, 0.03, slant=0.25, width=0.17)
    rump_stripes(k, ["loaf", "rump", "base", "hind"], 0.2, 0.29, 0.03, 0.052)
    forehead(k)
    cheek_stripes(k)

    # The ears: small, rounded, set on the head's top corners and turned a little out, pink inside
    L.add_ears(k, h=H, hr=HR, color=GREY, lining=EARIN, at=(0.62, -0.05, 0.66), tip=(0.028, 0.004, 0.062),
               base=0.037, depth=0.32, lining_share=0.66)

    add_brows(k)
    add_nose_mouth(k)
    add_thread(k, cy=-0.1, rx=0.123, ry=0.124, z0=0.152, dz=-0.046, r=0.0058)
    add_tail(k, TAIL, [0.03, 0.031, 0.03, 0.029, 0.027, 0.022], (0.28, 0.5, 0.72, 0.9), 0.024)

    k.build(voxel=0.0046, smooth=4, band=0.004, wband=0.03)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out_path)


if __name__ == "__main__":
    pisicut(*args())
