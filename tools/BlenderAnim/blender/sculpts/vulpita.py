# Vulpita sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/vulpita.py -- <the export's Vulpita.glb> <out.glb>
# Carpathia No. 10089, the Dark fox cub (docs/plans/decisions/CP-dex-2b-mountains-more.md, "89. Vulpita"). From its
# picture (docs/art/reference/gemini/carpathia/10089-Vulpita.jpg): a sitting fox cub in deep crimson red; huge tall
# pointed ears splayed out, dark maroon inside with black tufts, their top third black; narrow cunning amber eyes, their
# top lid a heavy straight line rising to the outer corner, a dark maroon mask line drawn out behind each and dark brows
# over them; a red stripe down the bridge of a short pointed snout, a small dark nose, a smirk; white lower face and
# cheeks spiking out in a ruff; a white bib from the throat down the chest; black stockings on all four legs; big round
# haunches; a great bushy tail curled up beside it, red with dark maroon flames, a black band and a big white tip.
# The picture raises one forepaw to the chest; the rest pose puts every foot down (carpathia's rule for an action).
# Reference ratios (picture, ears' tips to the feet): head height (crown to chin) / total 0.29 (here 0.31), head width
# with the ruff / body width 1.3 (here 1.3), ear length / total 0.27 (here 0.27), tail's bulk / total height 0.52 (here 0.53).
#
# Written to be grown from: vulpoi.py keeps this frame (a stage is a dict of sizes and colours, every part a function of
# the kit and the stage). fox(src, out, P, extra) builds a stage; extra(k, P) adds what a later stage grows.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, axes, tri,
                       args)


# Skeleton: body (0, 0, 0.2), head (0, -0.02, 0.32), ears (+-0.045, -0.02, 0.45), forelegs (+-0.05, -0.05, 0.22),
# hind legs (+-0.08, 0.06, 0.1), tail (0, 0.07, 0.12). The export stands 0.62 tall.
VULPITA = dict(
    name="Vulpita",
    height=0.62,
    voxel=0.0052,
    s=1.0,                       # every length below is multiplied by this (a stage grown from the cub)
    coat="#8A2228", deep="#5A161C", dark="#1C1416", white="#F4F0F0", shade="#D8D0D4", earin="#4E2428",
    nose="#2A1A1C", ink="#160E10", iris_top="#B8741E", iris_bottom="#F0B648", pupil="#1A0E08", glint="#FFFFFF",
    # the head
    H=(0, -0.05, 0.4), HR=(0.107, 0.095, 0.093),
    muzzle=((0, -0.074, -0.036), (0.042, 0.052, 0.033)),
    cheek=((0.062, -0.03, -0.042), (0.05, 0.05, 0.046)),
    # the trunk sitting up
    neck=((0, -0.045, 0.31), (0.074, 0.07, 0.08)),
    chest=((0, -0.052, 0.24), (0.086, 0.074, 0.1)),
    torso=((0, 0.02, 0.18), (0.094, 0.1, 0.115)),
    rump=((0, 0.07, 0.098), (0.1, 0.09, 0.09)),
    haunch=((0.074, 0.06, 0.098), (0.062, 0.088, 0.088)),
    # legs: the forelegs straight down before the chest, the hind feet forward beside them
    fore=[(0.048, -0.062, 0.215), (0.05, -0.086, 0.12), (0.05, -0.094, 0.04)], fore_r=[0.033, 0.029, 0.027],
    shoulder=((0.05, -0.055, 0.2), (0.042, 0.046, 0.062)),
    fpaw=((0.05, -0.114, 0.021), (0.033, 0.044, 0.022)),
    hind=[(0.086, 0.06, 0.07), (0.09, 0.01, 0.04), (0.092, -0.02, 0.024)], hind_r=[0.036, 0.03, 0.027],
    hpaw=((0.092, -0.044, 0.021), (0.033, 0.058, 0.022)),
    stock_fore=0.185, stock_hind=0.075, claws=None,
    # the tail: up from the rump, over and down round the +X side, its tip by the ground
    tail=[(0.0, 0.1, 0.09), (0.07, 0.19, 0.15), (0.16, 0.215, 0.245), (0.245, 0.155, 0.27), (0.275, 0.065, 0.195),
          (0.255, 0.0, 0.1), (0.215, -0.03, 0.055)],
    tail_r=[0.04, 0.064, 0.084, 0.09, 0.085, 0.07, 0.042],
    tail_white=0.115, tail_band=0.155, tip="white",
    # the ears
    ear_at=(0.55, 0.15, 0.72), ear_tip=(0.1, 0.012, 0.168), ear_w=0.05, ear_d=0.017, ear_black=0.62,
    # the face
    eye_x=0.05, eye_dz=0.012, eye_half=(0.031, 0.025), eye_tilt=1.25,
    wink=False,
    bib_top=0.33, bib_bottom=0.17, bib_w=(0.03, 0.055),
)
EYE = [(-1.0, -0.22), (-0.62, 0.12), (0.0, 0.42), (0.6, 0.66), (1.0, 0.6), (0.9, 0.16), (0.5, -0.4),
       (-0.15, -0.62), (-0.68, -0.5)]


def V3(a):
    return np.array(a, float)


def sc(P, a):
    return V3(a) * P["s"]


def on_head(x, z, h, hr, lift=0.0):
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / np.array(hr) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def on_ell(c, r, d):
    c, r, d = V3(c), V3(r), unit(*d)
    t = 1 / math.sqrt(((d / r) ** 2).sum())
    p = c + d * t
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spike(base, d, length, width, depth=None, across=None):
    """A solid point of fur, flat-faced, from a diamond of a root along d; across turns the diamond."""
    d = unit(*d)
    depth = depth or width * 0.6
    if across is None:
        a, b = axes(d)
    else:
        a = V3(across) - d * (V3(across) @ d); a /= np.linalg.norm(a)
        b = np.cross(d, a)
    base = V3(base)
    return hull([base + a * width, base - a * width, base + b * depth, base - b * depth, base - d * width * 0.5,
                 base + d * length])


# --- the body ---------------------------------------------------------------------------------------------------------
def add_body(k, P):
    for nm in ("neck", "chest", "torso", "rump"):
        c, r = P[nm]
        k.add(nm, ell(sc(P, c), sc(P, r)), P["coat"], "head" if nm == "neck" else "body")
    c, r = P["haunch"]
    k.add("haunch", ell(sc(P, c), sc(P, r)), P["coat"], "hind{s}", mirror=True)
    c, r = P["shoulder"]
    k.add("fshoulder", ell(sc(P, c), sc(P, r)), P["coat"], "leg{s}", mirror=True)


def add_legs(k, P):
    k.add("fleg", tube([sc(P, p) for p in P["fore"]], np.array(P["fore_r"]) * P["s"]), P["coat"], "leg{s}", mirror=True)
    c, r = P["fpaw"]
    k.add("fpaw", ell(sc(P, c), sc(P, r)), P["dark"], "leg{s}", mirror=True)
    k.add("hleg", tube([sc(P, p) for p in P["hind"]], np.array(P["hind_r"]) * P["s"]), P["coat"], "hind{s}", mirror=True)
    c, r = P["hpaw"]
    k.add("hpaw", ell(sc(P, c), sc(P, r)), P["dark"], "hind{s}", mirror=True)
    s = P["s"]
    # black stockings up the legs, their top edge in points of fur (as the picture's)
    def stock(line):
        def m(Q):
            ang = np.arctan2(Q[:, 1] + 0.05 * s, np.abs(Q[:, 0]) - 0.05 * s)
            return inside(Q[:, 2] - line * s - 0.012 * s * tri(ang * 1.3), 0.003)
        return m
    k.paint(stock(P["stock_fore"]), P["dark"], on=["fleg", "fpaw"])
    k.paint(stock(P["stock_hind"]), P["dark"], on=["hleg", "hpaw"])
    if P["claws"]:
        for bone, (c, r) in (("leg{s}", P["fpaw"]), ("hind{s}", P["hpaw"])):
            c, r = sc(P, c), sc(P, r)
            for i, t in enumerate((-0.5, 0.0, 0.5)):
                root = c + np.array([t * r[0], -r[1] * 0.85, -r[2] * 0.2])
                k.add(f"claw{bone[:3]}{i}", spike(root, (0, -0.75, -0.65), r[1] * 0.4, r[0] * 0.15, r[0] * 0.12,
                                                  across=(1, 0, 0)), P["claws"], bone, fuse=False, mirror=True,
                      surface="shell")


# --- the head: the skull, a short pointed snout, white cheeks --------------------------------------------------------------
def add_head(k, P):
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    k.add("head", ell(H, HR), P["coat"], "head")
    o, r = P["muzzle"]
    def taper(V):
        V = V.copy()
        f = 1 - 0.32 * np.clip(-V[:, 1], 0, 1)          # narrower toward the nose
        V[:, 0] *= f; V[:, 2] *= f
        return V
    k.add("muzzle", ell(H + sc(P, o), sc(P, r), deform=taper), P["white"], "head")
    o, r = P["cheek"]
    k.add("cheek", ell(H + sc(P, o), sc(P, r)), P["white"], "head", mirror=True)


def face_paint(k, P):
    """White below the eyes; the red stripe down the snout's bridge; the white bib down the chest."""
    s = P["s"]
    H = sc(P, P["H"])
    ez = H[2] + P["eye_dz"] * s
    def lower(Q):
        # below a line that dips under each eye and rises to the cheeks' sides
        line = ez - 0.03 * s + 0.02 * s * np.clip(np.abs(Q[:, 0]) / (0.09 * s), 0, 1)
        return inside(Q[:, 2] - line, 0.004) * inside(Q[:, 1] - H[1] + 0.01 * s, 0.01)
    k.paint(lower, P["white"], on=["head", "muzzle", "cheek", "neck"])
    o, r = P["muzzle"]
    mc = H + sc(P, o)
    def bridge(Q):
        w = 0.022 * s * (1 - 0.45 * np.clip((mc[1] - Q[:, 1]) / (0.05 * s), 0, 1))
        return (inside(np.abs(Q[:, 0]) - w, 0.003) * inside(mc[2] + 0.004 * s - Q[:, 2], 0.004)
                * inside(Q[:, 2] - ez + 0.004 * s, 0.004))
    k.paint(bridge, P["coat"], on=["head", "muzzle"])
    top, bot = P["bib_top"] * s, P["bib_bottom"] * s
    w0, w1 = P["bib_w"]
    def bib(Q):
        t = np.clip((top - Q[:, 2]) / (top - bot), 0, 1)
        w = (w1 + (w0 - w1) * t ** 1.5) * s
        w = w * (1 - 0.25 * np.abs(tri(Q[:, 2] / (0.03 * s))))
        return (inside(np.abs(Q[:, 0]) - w, 0.003) * inside(Q[:, 1] + 0.035 * s, 0.01)
                * inside(Q[:, 2] - top, 0.01) * inside(bot - Q[:, 2], 0.004))
    k.paint(bib, P["white"], on=["neck", "chest", "torso"])


def add_ruff(k, P):
    """White fur spiking out from each cheek and down the jaw, red tufts on the crown, white points under the bib."""
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    rng = np.random.RandomState(5)
    lens = (0.05, 0.062, 0.07, 0.064, 0.05, 0.038)
    for side in (1, -1):
        for i, ln in enumerate(lens):
            a = 0.75 + 0.3 * i                             # from the cheek's side round down under the jaw
            root = H + np.array([side * math.sin(a) * HR[0] * 0.92, -0.035 * s, -0.035 * s - math.cos(a) * HR[2] * 0.75])
            way = np.array([side * math.sin(a), -0.15, -math.cos(a) * 0.7 - 0.25]) + rng.normal(0, 0.08, 3)
            k.add(f"ruff{i}{'R' if side > 0 else 'L'}",
                  spike(root, way, ln * s * (0.92 + 0.2 * rng.rand()), 0.024 * s, 0.017 * s, across=(0, 1, 0)),
                  P["white"] if i % 2 == 0 else P["shade"], "head", fuse=False, surface="fur")
    for i, (x, d) in enumerate(((0.0, (0.05, 0.3, 1.0)), (0.025, (0.4, 0.2, 1.0)), (-0.025, (-0.35, 0.25, 1.0)))):
        root = H + np.array([x * s, 0.01 * s, HR[2] * 0.82])
        k.add(f"crown{i}", spike(root, d, (0.04 - 0.008 * abs(x) / 0.025) * s, 0.016 * s, 0.012 * s), P["coat"],
              "head", fuse=False, surface="fur")


# --- the ears: tall red points, a dark lining with black tufts, their top black -------------------------------------
def add_ears(k, P):
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    w, dp = P["ear_w"] * s, P["ear_d"] * s
    for side in (1, -1):
        nm = "R" if side > 0 else "L"
        m = np.array([side, 1, 1.0])
        root = (H + HR * V3(P["ear_at"])) * m
        tp = root + sc(P, P["ear_tip"]) * m
        d = tp - root; L = np.linalg.norm(d); d = d / L
        face = unit(side * 0.2, -1, 0.05); face = unit(*(face - d * (face @ d)))
        across = unit(*np.cross(d, face))

        def ring(t, ww, ff, off=0.0):
            c = root + d * L * t + face * off
            return [c + across * ww + face * ff, c - across * ww + face * ff, c + across * ww - face * ff,
                    c - across * ww - face * ff]
        def wid(t):
            return w * (1 - t) ** 0.85 + 0.002
        cut = P["ear_black"]
        k.add("ear" + nm, hull(ring(0.0, w, dp) + ring(0.35, wid(0.35), dp * 0.85) + ring(cut + 0.04, wid(cut + 0.04), dp * 0.6)),
              P["coat"], "ear" + nm, fuse=False, surface="fur")
        k.add("eartip" + nm, hull(ring(cut, wid(cut) * 1.06, dp * 0.66) + [tp + d * 0.004 * s]), P["dark"], "ear" + nm,
              fuse=False, surface="fur")
        # the lining, a little in front of the ear's face
        lp = []
        for t, f in ((0.1, 0.66), (0.45, 0.6), (0.78, 0.5)):
            lp += [root + d * L * t + across * wid(t) * f + face * (dp * 1.05),
                   root + d * L * t - across * wid(t) * f + face * (dp * 1.05),
                   root + d * L * t + across * wid(t) * f * 0.8 + face * (dp * 0.3),
                   root + d * L * t - across * wid(t) * f * 0.8 + face * (dp * 0.3)]
        lp.append(root + d * L * 0.9 + face * dp * 0.9)
        k.add("earin" + nm, hull(lp), P["earin"], "ear" + nm, fuse=False, surface="fur")
        # black fur in the lining: low flat points laid along it from its base, pointing up
        for i, (t, off) in enumerate(((0.06, 0.3), (0.06, -0.3), (0.2, 0.0))):
            base = root + d * L * t + across * wid(t) * off + face * dp * 1.05
            way = unit(*(d + across * off * 0.4))
            k.add(f"eartuft{i}{nm}", spike(base, way, L * 0.3, w * 0.26, dp * 0.35, across=across), P["dark"],
                  "ear" + nm, fuse=False, surface="fur")


# --- the tail: a great curled brush, dark flames along it, a black band and a white tip ---------------------------------
def tail_end(P):
    pts = [sc(P, p) for p in P["tail"]]
    return pts, pts[-1] + unit(*(pts[-1] - pts[-2])) * P["tail_r"][-1] * P["s"] * 0.6


def add_tail(k, P):
    s = P["s"]
    pts, end = tail_end(P)
    k.add("tail", tube(pts, np.array(P["tail_r"]) * s), P["coat"], "tail")
    # dark maroon flames over its top and outer side
    # broad flames of dark maroon over the upper curl, their points running down the tail
    # chevrons along the tail's length (arc length t, the angle round from its outer side), pointing to the tip
    line = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * f for i in range(len(pts) - 1) for f in np.linspace(0, 1, 30, endpoint=False)])
    arc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(line, axis=0), axis=1))])
    mid = line.mean(axis=0)
    def flames(Q):
        idx = np.argmin(((Q[:, None, :] - line[None, ::2]) ** 2).sum(2), axis=1) * 2
        idx = np.clip(idx, 0, len(line) - 2)
        c = line[idx]
        d = line[idx + 1] - line[idx]; d /= np.linalg.norm(d, axis=1, keepdims=True)
        outv = c - mid; outv -= d * (outv * d).sum(1, keepdims=True); outv /= np.linalg.norm(outv, axis=1, keepdims=True) + 1e-9
        r = Q - c; r -= d * (r * d).sum(1, keepdims=True); r /= np.linalg.norm(r, axis=1, keepdims=True) + 1e-9
        cosang = (r * outv).sum(1)
        ang = np.arccos(np.clip(cosang, -1, 1))
        t = arc[idx]
        band = np.abs(tri(t / (0.13 * s) + ang * 0.3))
        return (inside(band - 0.42, 0.06) * inside(ang - 1.7, 0.2) * inside(0.08 * s - t, 0.02)
                * inside(t - (arc[-1] - P["tail_band"] * s * 0.9), 0.02))
    k.paint(flames, P["deep"], on=["tail"])
    k.paint(lambda Q: inside(np.linalg.norm(Q - end, axis=1) - P["tail_band"] * s, 0.004), P["dark"], on=["tail"])
    if P["tip"] == "white":
        def tipmask(Q):
            dd = np.linalg.norm(Q - end, axis=1)
            return inside(dd - P["tail_white"] * s - 0.008 * s * tri(np.arctan2(Q[:, 2] - end[2], Q[:, 0] - end[0]) * 1.8), 0.003)
        k.paint(tipmask, P["white"], on=["tail"])
        # the white brush's points beyond the tip
        d = unit(*(pts[-1] - pts[-2]))
        a, b = axes(d)
        for i in range(7):
            th = 2 * math.pi * i / 7
            way = unit(*(d + 0.55 * (math.cos(th) * a + math.sin(th) * b)))
            root = end - d * 0.03 * s + (math.cos(th) * a + math.sin(th) * b) * 0.02 * s
            k.add(f"tailtip{i}", spike(root, way, (0.05 + 0.012 * (i % 2)) * s, 0.022 * s, 0.016 * s),
                  P["white"] if i % 2 else P["shade"], "tail", fuse=False, surface="fur")
    # tufts along the tail's outer edge so its outline reads as fur
    rng = np.random.RandomState(9)
    for i, t in enumerate((1.4, 2.0, 2.6, 3.2, 3.8)):
        j = int(t); f = t - j
        c = pts[j] * (1 - f) + pts[j + 1] * f
        rr = (P["tail_r"][j] * (1 - f) + P["tail_r"][j + 1] * f) * s
        d = unit(*(pts[j + 1] - pts[j]))
        out = c - V3((0.14, 0.12, 0.2)) * s
        out = -(out - d * (out @ d)); out = unit(*out) if np.linalg.norm(out) > 1e-6 else unit(1, 0, 0)
        root = c + out * rr * 0.8
        k.add(f"tailfur{i}", spike(root, unit(*(out + d * 0.6 + rng.normal(0, 0.1, 3))), 0.042 * s, 0.024 * s, 0.017 * s),
              P["coat"] if i % 2 else P["deep"], "tail", fuse=False, surface="fur")


# --- the face's marks: the dark mask line behind each eye, the brows, the nose, the smirk ---------------------------------
def add_marks(k, P):
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    eps = P["height"] * 0.004
    ez = H[2] + P["eye_dz"] * s
    for side in (1, -1):
        # the mask line drawn out and up from each eye's outer corner
        p, n = on_head(side * 0.082 * s, ez + 0.014 * s, H, HR)
        k.decal(p, n, (0.02 * s, 0.009 * s), P["deep"], up=unit(-side * 0.45, 0, 1), eps=eps, rings=8, segs=32)
        # the brows: dark strokes over the eyes, low toward the nose
        p, n = on_head(side * 0.052 * s, ez + 0.04 * s, H, HR)
        k.decal(p, n, (0.025 * s, 0.006 * s), P["deep"], up=unit(-side * 0.42, 0, 1), eps=eps, rings=8, segs=32,
                outline=polygon([(-1, -0.4), (0, 1), (1, 0.6), (0.2, -0.6)]))
    # a darker saddle down the back
    def saddle(Q):
        return (inside(np.abs(Q[:, 0]) - 0.045 * s - 0.03 * s * np.clip((0.3 * s - Q[:, 2]) / (0.2 * s), 0, 1), 0.02)
                * inside(-Q[:, 1] + 0.02 * s, 0.02) * inside(Q[:, 2] - 0.33 * s, 0.02) * inside(0.08 * s - Q[:, 2], 0.02))
    k.paint(saddle, P["deep"], on=["torso", "rump", "neck", "chest"])
    o, r = P["muzzle"]
    mc, mr = H + sc(P, o), sc(P, r)
    tipp = mc + np.array([0, -mr[1] * 0.7, mr[2] * 0.48])
    k.decal(tipp, unit(0, -1, 0.5), (0.016 * s, 0.012 * s), P["nose"], eps=eps * 1.2,
            outline=polygon([(-1, 0.7), (1, 0.7), (0.0, -1)]), rings=6, segs=28)
    CW, CI = srgb(P["white"]), srgb(P["ink"])
    def smirk(u, v):
        c = np.tile(CW, (len(u), 1))
        line = -0.25 + 0.75 * u * u + 0.28 * u
        c[(np.abs(v - line) < 0.2) & (np.abs(u) < 0.92)] = CI
        c[(np.abs(u) < 0.1) & (v > -0.25) & (v < 0.6)] = CI
        return c
    k.decal(mc + np.array([0, -mr[1] * 0.68, -mr[2] * 0.42]), unit(0, -1, -0.25), (0.03 * s, 0.014 * s), smirk,
            eps=eps, rings=12, segs=48)


# --- the eyes ---------------------------------------------------------------------------------------------------------
def eye_colour(P, half_shut=False):
    CT, CB, CP, CG, CI, CL = (srgb(P[c]) for c in ("iris_top", "iris_bottom", "pupil", "glint", "ink", "coat"))
    rim = polygon(EYE)
    def eye(u, v):
        t = np.clip((0.6 - v) / 1.2, 0, 1)[:, None]
        c = CT[None] * (1 - t) + CB[None] * t
        c[np.hypot((u - 0.05) / 0.22, (v + 0.02) / 0.46) < 1] = CP
        c[np.hypot((u - 0.3) / 0.15, (v - 0.22) / 0.15) < 1] = CG
        rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[rr > 0.86] = CI
        top = 0.1 + 0.3 * (u + 1)                          # the heavy line of the top lid, rising outward
        c[(v > top - 0.08) & (rr > 0.5)] = CI
        if half_shut:
            lid = 0.02 + 0.08 * u
            c[v > lid] = CL
            c[np.abs(v - lid) < 0.14] = CI
        return c
    return eye


def lay_eyes(k, P, state):
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    p, n = on_head(P["eye_x"] * s, H[2] + P["eye_dz"] * s, H, HR)
    n = unit(n[0] * P["eye_tilt"], n[1], n[2])
    lid, ink = srgb(P["coat"]), srgb(P["ink"])
    half = (P["eye_half"][0] * s, P["eye_half"][1] * s)
    eps = P["height"] * 0.0035
    for side in (1, -1):
        wink = P["wink"] and side < 0 and state == "Open"
        look = eye_look(state, eye_colour(P, wink), lid, ink, line=0.17)
        q = np.array([side, 1, 1.0])
        out = EYE if side > 0 else [(-a, b) for a, b in EYE]
        k.decal(p * q + np.array([0.0007 * side, 0, 0]), n * q, half,
                look if side > 0 else (lambda u, v, f=look: f(-u, v)), eps=eps, outline=polygon(out), rings=20,
                segs=56)


# --- the board check --------------------------------------------------------------------------------------------------
def board(ob):
    me = ob.data
    co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    h = co[:, 2].max(); w = co[:, 0].max() - co[:, 0].min(); l = co[:, 1].max() - co[:, 1].min()
    square = 1.3 * max(h, 0.85 * max(w, l))
    corners = 0.853 * w + 0.522 * l
    print(f"KIT: board: height {h:.3f} width {w:.3f} length {l:.3f}; four-corner sum {corners:.3f} of a square "
          f"{square:.3f} ({corners / square:.2f})")


def fox(src, out_path, P, extra=None):
    k = Kit(src, coat="fur")
    add_body(k, P)
    add_legs(k, P)
    add_head(k, P)
    add_ears(k, P)
    add_ruff(k, P)
    add_tail(k, P)
    if extra:
        extra(k, P)
    face_paint(k, P)
    add_marks(k, P)
    ob = k.build(voxel=P["voxel"], smooth=4, band=P["voxel"] * 0.8, wband=P["height"] * 0.06)
    board(ob)
    k.eye_states(lambda st: lay_eyes(k, P, st))
    k.export(out_path)


if __name__ == "__main__":
    fox(*args(), VULPITA)
