# Lynkit sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lynkit.py -- <the export's Lynkit.glb> <out.glb>
# Carpathia No. 10001, the Grass starter (docs/plans/decisions/CP-dex-1-plateau.md, "1. Lynkit"). From its picture
# (docs/art/reference/gemini/carpathia/10001-Lynkit.jpg): a small round lynx kitten in soft moss green, darker green
# spots over the back, the flanks, the haunches and the crown; a cream chest bib from the chin to the belly, a cream
# muzzle and lower face, a cream belly; four big round cream paws, too big for it, with toes; a pale yellow-green ruff
# of fluffy points out from each cheek and down the sides of the jaw (the throat between stays cream); big pointed ears
# standing up, lined cream-tan, each topped by a little brush of dark fir needles; big round amber eyes with a dark rim,
# a large dark pupil and a white glint; a small grey nose, a little mouth; a short stubby tail curling up, its tip black.
# Reference ratios (picture, to the needles' tips): head height / total 0.34 (here 0.37: a tenth larger), head width /
# body width about 1.3 (here 1.3), leg showing under the belly / total 0.26 (here 0.21: the belly lowered against
# stilts), paw width / head width 0.38 (here 0.37), ear with its needles / head height 1.1 (here 1.0).
#
# Written to be grown from: firlynx.py and codrulynx.py keep this frame (a stage is a dict of sizes and colours, every
# part a function of the kit and the stage), the way the ears, the fir brushes, the ruff, the eyes, the nose, the mouth,
# the paws and the bib are made. lynx(src, out, P, extra) builds a stage; extra(k, P) adds what a later stage grows.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, smoothstep, unit, eye_look, polygon, axes, args)


# --- the stage: Lynkit's sizes (metres, Blender's axes: front -Y, up +Z, the model's left -X) and colours ----------------
# Skeleton: body (0, 0.03, 0.22), head (0, -0.092, 0.288), ears (+-0.071, -0.134, 0.432), forelegs (+-0.07, -0.09, 0.15),
# hind legs (+-0.07, 0.11, 0.15), tail (0, 0.174, 0.264).
LYNKIT = dict(
    name="Lynkit",
    height=0.64,
    voxel=0.0055,
    # colours
    coat="#8DB46E", spot="#557F45", cream="#F3EDCC", ruff="#BCD472", ruff2="#D2E296", fir="#3C6B38", fir2="#4E8044",
    earin="#E0D2B2", nose="#7C6C66", ink="#1E1A16", tailtip="#24221F", pawc="#F3EDCC", tailunder="#F3EDCC",
    # the trunk: chest, middle, rump (centre, radii)
    trunk=[((0, -0.07, 0.25), (0.105, 0.1, 0.108)), ((0, 0.02, 0.245), (0.1, 0.11, 0.104)),
           ((0, 0.1, 0.25), (0.106, 0.09, 0.104))],
    neck=((0, -0.12, 0.31), (0.082, 0.07, 0.08)),
    # the head
    H=(0, -0.152, 0.392), HR=(0.135, 0.12, 0.126),
    muzzle=((0, -0.1, -0.058), (0.056, 0.04, 0.038)),        # offset from H, radii
    # legs
    leg_x=0.072, fore_y=-0.09, hind_y=0.11,
    shoulder=((0.0, 0.0, 0.19), (0.05, 0.056, 0.075)),       # offsets from (leg_x, fore_y, 0), radii
    thigh=((0.0, 0.008, 0.205), (0.06, 0.072, 0.085)),
    leg=[(0.0, 0.0, 0.17), (0.0, -0.004, 0.1), (0.0, -0.006, 0.045)], leg_r=[0.037, 0.034, 0.034],
    hind=[(0.0, 0.01, 0.17), (0.0, 0.012, 0.1), (0.0, 0.006, 0.045)], hind_r=[0.04, 0.035, 0.034],
    paw=((0.002, -0.024, 0.028), (0.05, 0.06, 0.03)),
    paw_line=0.058, toes=True, cuff=None, claws=None,
    # the tail: its line (authored in the midline plane, turned out toward +X) and radii
    tail=[(0, 0.17, 0.27), (0, 0.215, 0.29), (0, 0.245, 0.325), (0, 0.255, 0.362)], tail_r=[0.034, 0.037, 0.04, 0.038],
    tail_out=unit(0.42, 0.91, 0), tail_tip=0.05,
    # the ears and their brushes of needles
    ear_at=(0.6, 0.05, 0.7), ear_tip=(0.034, 0.006, 0.105), ear_base=0.048, ear_depth=0.32,
    brush=0.085, brush_w=0.0115,
    # the ruff: (angle from straight down, length) of each point, and a puff a side
    ruff_pts=[(0.55, 0.05), (0.8, 0.064), (1.05, 0.074), (1.3, 0.074), (1.55, 0.066), (1.8, 0.054), (2.05, 0.042)],
    ruff_w=0.03, ruff_ring=(0.92, -0.025, 0.84, -0.035), puff=((0.098, -0.01, -0.055), (0.036, 0.042, 0.055)),
    # the face
    eye_x=0.058, eye_dz=0.004, eye_half=(0.034, 0.038), eye_tilt=1.2, eye="round",
    iris_top="#8A4A1C", iris_bottom="#E59A38", pupil="#26140A", glint="#FFFFFF", eye_ink="#1A140E",
    nose_half=(0.011, 0.008), mouth_half=(0.02, 0.012),
    # cream: the lower face (centre offset from H and radii of an ellipsoid), the bib, the belly
    face=((0, -0.085, -0.07), (0.086, 0.08, 0.064)),
    bib=dict(y=-0.13, top=0.36, bottom=0.15, w0=0.05, w1=0.07),
    belly=0.165,
    spots=True, stripes=None,
)


# --- shared helpers -----------------------------------------------------------------------------------------------------
def V3(a):
    return np.array(a, float)


def on_head(x, z, h, hr, lift=0.0):
    """The head's front point at (x, z) on its ellipsoid, lifted along its normal, and the normal."""
    zn = (z - h[2]) / hr[2]
    q = max(0.0, 1 - zn * zn - (x / hr[0]) ** 2)
    p = np.array([x, h[1] - hr[1] * math.sqrt(q), z])
    n = (p - h) / np.array(hr) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def on_ell(c, r, d):
    """The point of an ellipsoid in direction d from its centre, and its outward normal."""
    c, r, d = V3(c), V3(r), unit(*d)
    t = 1 / math.sqrt(((d / r) ** 2).sum())
    p = c + d * t
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def spike(base, d, length, width, depth=None, across=None):
    """A solid point (fur, a needle), flat-faced, from a diamond of a root along d; across turns the diamond."""
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


# --- the trunk, the neck, the head and its muzzle -----------------------------------------------------------------------
def add_body(k, P):
    for i, (c, r) in enumerate(P["trunk"]):
        k.add(("chest", "middle", "rump")[i], ell(c, r), P["coat"], "body")
    c, r = P["neck"]
    k.add("neck", ell(c, r), P["coat"], "head")


def add_head(k, P):
    H, HR = V3(P["H"]), V3(P["HR"])
    k.add("head", ell(H, HR), P["coat"], "head")
    o, r = P["muzzle"]
    k.add("muzzle", ell(H + V3(o), r), P["cream"], "head")


# --- legs: a shoulder and a thigh fused over the top of each, a thick leg, a big paw ----------------------------------
def add_legs(k, P):
    x = P["leg_x"]
    # The paws' colour up to a crisp line round each leg (before the toes, which are painted over it)
    if P["pawc"] != P["coat"]:
        k.paint(lambda Q: inside(Q[:, 2] - P["paw_line"], 0.0025), P["pawc"], on=["fore", "hind"])
    for bone, y, nm, top, pts, rr in (("leg{s}", P["fore_y"], "fore", P["shoulder"], P["leg"], P["leg_r"]),
                                      ("hind{s}", P["hind_y"], "hind", P["thigh"], P["hind"], P["hind_r"])):
        base = V3((x, y, 0))
        (o, r) = top
        k.add(f"{nm}top", ell(base + V3(o), r), P["coat"], bone, mirror=True)
        k.add(f"{nm}leg", tube([base + V3(p) for p in pts], rr), P["coat"], bone, mirror=True)
        o, r = P["paw"]
        k.add(f"{nm}paw", ell(base + V3(o), r), P["pawc"], bone, mirror=True)
        if P["toes"]:
            add_toes(k, P, base + V3(o), r)
        if P["claws"]:
            add_claws(k, P, base + V3(o), r, bone)
        if P["cuff"]:
            add_cuff(k, P, base + V3(pts[-1]), rr[-1], bone, nm)


def add_toes(k, P, pc, r):
    """Two dark grooves over the front of a paw between its three toes: paint on the paw (a patch cast from the
    front lands on the leg in front of a hind paw)."""
    col = P["spot"] if P["pawc"] == P["coat"] else P.get("toe", "#B5AA8A")
    def toes(Q):
        ax = np.abs(Q[:, 0]) - abs(pc[0])
        front = Q[:, 1] < pc[1] - r[1] * 0.25
        return inside(np.abs(np.abs(ax) - r[0] * 0.3) - r[0] * 0.07, 0.0015) * front * inside(Q[:, 2] - pc[2] - r[2] * 0.55, 0.003)
    k.paint(toes, col, on=["forepaw" if pc[1] < 0 else "hindpaw"])


def add_claws(k, P, pc, r, bone):
    """Pale claws out of the front of a paw: solid hooks, three a paw."""
    for s, side in ((1, "R"), (-1, "L")):
        for i, t in enumerate((-0.55, 0.0, 0.55)):
            root = pc * np.array([s, 1, 1]) + np.array([t * r[0] * s, -r[1] * 0.82, -r[2] * 0.15])
            k.add(f"claw{bone[:4]}{i}{side}", spike(root, (0, -0.7, -0.7), r[1] * 0.45, r[0] * 0.16, r[0] * 0.12,
                                                     across=(1, 0, 0)),
                  P["claws"], bone.format(s=side), fuse=False, surface="shell")


def add_cuff(k, P, ankle, rr, bone, nm):
    """A cuff of moss round the ankle: a lumpy fused ring and short points hanging over the paw."""
    rng = np.random.RandomState(7 if nm == "fore" else 11)
    c = ankle + np.array([0, 0, rr * 0.9])
    k.add(f"{nm}cuff", ell(c, (rr * 1.35, rr * 1.35, rr * 0.8)), P["cuff"], bone, mirror=True)
    for i in range(7):
        a = 2 * math.pi * i / 7 + 0.4
        o = np.array([math.sin(a), -math.cos(a), 0])
        root = c + o * rr * 1.1
        k.add(f"{nm}moss{i}", spike(root, o * 0.8 + np.array([0, 0, -0.75]) + rng.normal(0, 0.12, 3),
                                    rr * (0.75 + 0.3 * rng.rand()), rr * 0.48, rr * 0.3),
              P["cuff"], bone, fuse=False, mirror=True, surface="leaf")


# --- the tail: a stub curling up (longer and lower as the line grows), its tip dark ------------------------------------
def tail_pts(P):
    t = P["tail"]
    return [np.array([0, t[0][1], z]) + P["tail_out"] * (y - t[0][1]) for x, y, z in t]


def add_tail(k, P):
    pts = tail_pts(P)
    k.add("tail", tube(pts, P["tail_r"]), P["coat"], "tail")
    end = pts[-1] + unit(*(pts[-1] - pts[-2])) * P["tail_r"][-1] * 0.7
    k.paint(lambda Q: inside(np.linalg.norm(Q - end, axis=1) - P["tail_tip"], 0.003), P["tailtip"], on=["tail"])


# --- the ears: big pointed triangles from the head's top corners, a cream lining, a brush of fir on each tip ----------
def ear_frame(P, s):
    H, HR = V3(P["H"]), V3(P["HR"])
    m = np.array([s, 1, 1.0])
    root = (H + HR * V3(P["ear_at"])) * m
    tip = root + V3(P["ear_tip"]) * m
    return root, tip


def add_ears(k, P):
    base, dp = P["ear_base"], P["ear_base"] * P["ear_depth"]

    def prof(t):
        if t <= 0.5:
            f = t / 0.5
            return base * (1 - 0.42 * f), dp * (1 - 0.25 * f)
        f = (t - 0.5) / 0.5
        return base * 0.58 * (1 - f) + 0.002, dp * 0.75 * (1 - f) + 0.002

    for s in (1, -1):
        side = "R" if s > 0 else "L"
        root, tp = ear_frame(P, s)
        d = tp - root; L = np.linalg.norm(d); d = d / L
        across = unit(*np.cross(d, (0, 1.0, 0)))
        face = unit(0, -1, 0.1); face = unit(*(face - d * (face @ d)))
        pts = []
        for t in (0.0, 0.5):
            w, f = prof(t)
            c = root + d * L * t
            pts += [c + across * w + face * f, c - across * w + face * f, c + across * w - face * f, c - across * w - face * f]
        pts.append(tp)
        k.add("ear" + side, hull(pts), P["coat"], "ear" + side, fuse=False, surface="fur")
        lp = []
        for t in (0.12, 0.5, 0.82):
            w, f = prof(t)
            w *= 0.64 if t < 0.7 else 0.42
            c = root + d * L * t
            for off in (f + dp * 0.14, f - dp * 0.3):
                lp += [c + across * w + face * off, c - across * w + face * off]
        lp.append(root + d * L * 0.93 + face * (prof(0.93)[1] + dp * 0.1))
        k.add("earin" + side, hull(lp), P["earin"], "ear" + side, fuse=False, surface="fur")
        add_brush(k, P, tp, d, s, "ear" + side)


def add_brush(k, P, tp, d, s, bone, name="brush"):
    """A brush of fir needles on an ear's tip: a spine and two pairs of needles fanning up and out, solid points."""
    L, w = P["brush"], P["brush_w"]
    up = unit(*(d * 0.5 + np.array([0.12 * s, 0, 1.0])))
    across = unit(1, 0, -0.1 * s)
    base = tp - up * L * 0.12
    side = bone[-1]
    k.add(f"{name}0{side}", spike(base, up, L, w, w * 0.65, across=across), P["fir"], bone, fuse=False, surface="leaf")
    for i, (t, ang, ln) in enumerate(((0.12, 0.42, 0.78), (0.3, 0.34, 0.66), (0.5, 0.26, 0.5))):
        for j, sgn in enumerate((1, -1)):
            way = unit(*(up * math.cos(ang) + across * sgn * math.sin(ang) + np.array([0, (j - 0.5) * 0.25, 0])))
            root = base + up * L * t
            k.add(f"{name}{i + 1}{j}{side}", spike(root, way, L * ln, w * 0.82, w * 0.55, across=np.cross(way, (0, 1, 0))),
                  P["fir"] if (i + j) % 2 else P["fir2"], bone, fuse=False, surface="leaf")


# --- the ruff: fluffy points out from each cheek and down the sides of the jaw, a puff under them ---------------------
def add_ruff(k, P):
    H, HR = V3(P["H"]), V3(P["HR"])
    rx, ry, rz, dz = P["ruff_ring"]
    rng = np.random.RandomState(3)
    for s in (1, -1):
        side = "R" if s > 0 else "L"
        o, r = P["puff"]
        k.add("puff" + side, outward(ell(H + V3(o) * np.array([s, 1, 1]), r)), P["ruff"], "head", fuse=False,
              surface="fur")
        # two rows: the front row out from the cheeks, the back row behind it, its points between the front's
        for row, (dy, da, f) in enumerate(((0.0, 0.0, 1.0), (HR[1] * 0.2, 0.13, 0.85))):
            for i, (a, ln) in enumerate(P["ruff_pts"]):
                a = a + da
                root = H + np.array([s * math.sin(a) * HR[0] * rx, ry + dy, dz - math.cos(a) * HR[2] * rz])
                way = np.array([s * math.sin(a) * 0.8, 0.1 + 0.2 * row, -math.cos(a)]) + np.array([0, 0.05, -0.7])
                way = way + rng.normal(0, 0.1, 3)
                k.add(f"ruff{row}{i}{side}", spike(root, way, ln * f * (0.9 + 0.25 * rng.rand()), P["ruff_w"],
                                                   P["ruff_w"] * 0.8, across=(0, 1, 0)),
                      P["ruff"] if (i + row) % 2 else P["ruff2"], "head", fuse=False, surface="fur")


# --- cream: the lower face, the bib from chin to belly, the belly ----------------------------------------------------
def add_cream(k, P):
    H = V3(P["H"])
    o, r = P["face"]
    c, r = H + V3(o), V3(r)
    k.paint(lambda Q: inside(np.sqrt((((Q - c) / r) ** 2).sum(1)) - 1, 0.06), P["cream"], on=["head", "muzzle", "neck"])
    b = P["bib"]
    def bib(Q):
        t = np.clip((b["top"] - Q[:, 2]) / (b["top"] - b["bottom"]), 0, 1)
        w = b["w0"] + (b["w1"] - b["w0"]) * t
        # the bib's sides end in points of fur, as the picture's
        w = w * (1 - 0.18 * np.abs(np.sin(Q[:, 2] * 120)))
        return inside(np.abs(Q[:, 0]) - w, 0.004) * inside(Q[:, 1] - b["y"], 0.012) * inside(Q[:, 2] - b["top"], 0.01)
    k.paint(bib, P["cream"], on=["neck", "chest", "head"])
    belly = P["belly"]
    k.paint(lambda Q: inside(Q[:, 2] - belly, 0.008), P["cream"], on=["chest", "middle", "rump"])


# --- spots: darker green over the back, the flanks, the haunches; a few on the crown ----------------------------------
def add_spots(k, P):
    rng = np.random.RandomState(21)
    trunk = P["trunk"]
    c = V3(trunk[1][0]); r = np.array([trunk[1][1][0] * 1.02, 0.19, trunk[1][1][2] * 1.04])
    eps = P["height"] * 0.004
    for s in (1, -1):
        for i in range(13):
            yy = -0.75 + 1.5 * rng.rand()
            ang = 0.25 + 1.25 * rng.rand()              # from the top round toward the flank
            d = (s * math.sin(ang), yy, math.cos(ang))
            p, n = on_ell(c, r, d)
            size = 0.012 + 0.01 * rng.rand()
            k.decal(p, n, (size, size * 0.72), P["spot"], up=unit(0, 1, 0.2 * s), eps=eps, rings=5, segs=22)
        # on the thigh and the shoulder
        for (y, z, sz) in ((0.115, 0.23, 0.014), (0.14, 0.19, 0.011), (-0.095, 0.22, 0.011)):
            p = np.array([s * 0.12, y, z])
            k.decal(p, (s, 0.05, 0.25), (sz, sz * 0.75), P["spot"], up=unit(0, 1, 0.3), eps=eps, rings=5, segs=22)
    # on the crown, as the picture: a larger one high on the forehead, two smaller each side, two up between the ears
    H, HR = V3(P["H"]), V3(P["HR"])
    for x, dz, hs in ((0.0, 0.07, (0.015, 0.011)), (0.045, 0.078, (0.01, 0.009)), (-0.045, 0.078, (0.01, 0.009)),
                      (0.02, 0.105, (0.008, 0.007)), (-0.02, 0.105, (0.008, 0.007))):
        p, n = on_head(x, H[2] + dz, H, HR)
        k.decal(p, n, hs, P["spot"], eps=eps, rings=5, segs=22)
    for s in (1, -1):
        for d, hs in (((s * 0.4, 0.8, 0.3), (0.012, 0.009)), ((s * 0.15, 0.9, 0.05), (0.009, 0.008))):
            p, n = on_ell(H, HR, d)
            k.decal(p, n, hs, P["spot"], eps=eps, rings=5, segs=22)


# --- the face: nose, mouth, eyes --------------------------------------------------------------------------------------
def add_nose_mouth(k, P):
    H = V3(P["H"])
    o, r = P["muzzle"]
    mc = H + V3(o)
    front = mc + np.array([0, -r[1], 0])
    eps = P["height"] * 0.004
    nh = P["nose_half"]
    k.decal(front + np.array([0, 0.004, r[2] * 0.42]), unit(0, -1, 0.35), nh, P["nose"], eps=eps * 1.2,
            outline=polygon([(-1, 0.75), (1, 0.75), (0, -1)]), rings=6, segs=24)
    CG, CI = srgb(P["cream"]), srgb(P["ink"])
    def mouth(u, v):
        c = np.tile(CG, (len(u), 1))
        c[(np.abs(v - (-0.3 + 1.5 * (np.abs(u) - 0.45) ** 2)) < 0.2) & (np.abs(u) < 0.9) & (v < 0.3)] = CI
        c[(np.abs(u) < 0.12) & (v > 0.0)] = CI
        return c
    mh = P["mouth_half"]
    k.decal(front + np.array([0, 0.006, -r[2] * 0.25]), unit(0, -1, -0.15), mh, mouth, eps=eps, rings=10, segs=48)


ROUND = None


def almond(n=36, top=0.92, bottom=0.82, outer=0.3):
    """A smooth almond's outline (u outward, v up): fuller over the top, its outer corner drawn out and up a little."""
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        u, v = math.cos(t), math.sin(t)
        v *= (top if v > 0 else bottom) * (1 - 0.18 * u * u)
        v += outer * 0.25 * max(u, 0) ** 3
        pts.append((u, v))
    return pts
ALMOND = [(-1.0, 0.05), (-0.62, 0.6), (0.0, 0.92), (0.62, 0.82), (1.0, 0.38), (0.86, -0.4), (0.42, -0.82),
          (-0.2, -0.86), (-0.7, -0.55)]


def eye_colour(P):
    CT, CB, CP, CG, CI = (srgb(P[c]) for c in ("iris_top", "iris_bottom", "pupil", "glint", "eye_ink"))
    rim = polygon(P["eye_outline"]) if P.get("eye_outline") else None
    slit = P.get("slit", False)
    pr = P.get("pupil_r", 0.5)
    def eye(u, v):
        t = np.clip((0.55 - v) / 1.2, 0, 1)[:, None]
        c = CT[None] * (1 - t) + CB[None] * t
        if slit:
            c[np.hypot(u / 0.17, (v + 0.02) / 0.68) < 1] = CP
        else:
            c[np.hypot(u / pr, (v + 0.02) / (pr * 1.08)) < 1] = CP
        c[np.hypot((u - 0.24) / 0.2, (v - 0.32) / 0.2) < 1] = CG
        if rim is None:
            rr = np.hypot(u, v)
        else:
            rr = np.hypot(u, v) / np.array([rim(t_) for t_ in np.arctan2(v, u)])
        c[rr > P.get("rim", 0.84)] = CI
        if P.get("lid_line"):
            c[(rr > 0.62) & (v > 0.1)] = CI                  # the heavy dark line along the top lid
        return c
    return eye


def lay_eyes(k, P, state):
    H, HR = V3(P["H"]), V3(P["HR"])
    p, n = on_head(P["eye_x"], H[2] + P["eye_dz"], H, HR)
    n = unit(n[0] * P["eye_tilt"], n[1], n[2])
    lid = srgb(P.get("eye_lid", P["coat"]))
    ink = srgb(P.get("shut_ink", P["eye_ink"]))
    look = eye_look(state, eye_colour(P), lid, ink, line=0.16)
    if state == "Fierce" and P.get("fierce"):
        look = P["fierce"](eye_colour(P), lid, ink)
    outline = P.get("eye_outline")
    eps = P["height"] * 0.005
    k.decal(p, n, P["eye_half"], look, eps=eps, outline=polygon(outline) if outline else None, rings=20, segs=56,
            up=unit(-0.0, 0, 1))
    q = np.array([-1, 1, 1.0])
    k.decal(p * q, n * q, P["eye_half"], lambda u, v: look(-u, v), eps=eps,
            outline=polygon([(-a, b) for a, b in outline]) if outline else None, rings=20, segs=56)


# --- the board check: does the model fit the game's cell ----------------------------------------------------------------
def board(ob):
    me = ob.data
    co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    h = co[:, 2].max(); w = co[:, 0].max() - co[:, 0].min(); l = co[:, 1].max() - co[:, 1].min()
    square = 1.3 * max(h, 0.85 * max(w, l))
    corners = 0.853 * w + 0.522 * l
    print(f"KIT: board: height {h:.3f} width {w:.3f} length {l:.3f}; four-corner sum {corners:.3f} of a square "
          f"{square:.3f} ({corners / square:.2f})")


# --- a stage built --------------------------------------------------------------------------------------------------------
def lynx(src, out_path, P, extra=None, decals=None):
    k = Kit(src, coat="fur")
    add_body(k, P)
    add_legs(k, P)
    add_head(k, P)
    add_ears(k, P)
    add_ruff(k, P)
    add_tail(k, P)
    if P["stripes"]:
        P["stripes"](k, P)
    add_cream(k, P)
    if P["spots"]:
        add_spots(k, P)
    if extra:
        extra(k, P)
    add_nose_mouth(k, P)
    if decals:
        decals(k, P)
    ob = k.build(voxel=P["voxel"], smooth=4, band=P["voxel"] * 0.8, wband=P["height"] * 0.06)
    board(ob)
    k.eye_states(lambda s: lay_eyes(k, P, s))
    k.export(out_path)


if __name__ == "__main__":
    lynx(*args(), LYNKIT)
