# Veverel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/veverel.py -- <the export's Veverel.glb> <out.glb>
# Carpathia No. 10091, the Normal squirrel kit (docs/plans/decisions/CP-dex-2b-mountains-more.md, "91. Veverel"). From
# its picture (docs/art/reference/gemini/carpathia/10091-Veverel.jpg): a tiny red squirrel kit sitting up on its hind
# legs, bright orange-red; a round head with great puffed cream cheeks, a cream muzzle with a small pink nose and two
# buck teeth; big round dark eyes with a wide glint, a cream ring round each; long narrow ears standing up, lined pink,
# each ending in a dark brush; a cream belly; little arms holding a big hazelnut; long hind feet; a huge plume of a tail
# rising behind it in an S and curling over at its top, darker rust with darker swirls.
# The picture holds the nut at the mouth; here it is held under the chin (nothing covers the face in the game).
# Reference ratios (picture, the ear brushes to the feet): head height / total 0.26 (here 0.28), ears with brushes /
# total 0.4 (here 0.38), head width with the cheeks / body width 1.1 (here 1.12), tail height / total 0.7 (here 0.7).
#
# Written to be grown from: veverar.py keeps this frame (a stage is a dict of sizes and colours, every part a function
# of the kit and the stage). squirrel(src, out, P, extra) builds a stage; extra(k, P) adds what a later stage grows.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, hull, outward, srgb, inside, unit, eye_look, polygon, axes, tri, args)


# Skeleton: body (0, 0, 0.18), arms (+-0.05, -0.03, 0.24), head (0, -0.02, 0.29), ears (+-0.03, -0.02, 0.385),
# legs (+-0.06, 0.02, 0.1), tail (0, 0.06, 0.1). The export stands 0.55 tall.
VEVEREL = dict(
    name="Veverel",
    height=0.55,
    voxel=0.0046,
    s=1.0,
    coat="#DE5A2C", cream="#F6DFA8", tailc="#B04A2A", swirl="#7A2C18", tuft="#3A2018", earin="#E8A090",
    nose="#C86A5C", ink="#160E0A", iris="#2E1A10", white="#FFFFFF", nut="#8A5A30", nutcap="#5A3A1E",
    tooth="#FFFFFF",
    # the head and its cheeks
    H=(0, -0.035, 0.302), HR=(0.08, 0.072, 0.072),
    cheek=((0.054, -0.034, -0.032), (0.05, 0.046, 0.043)),
    muzzle=((0, -0.07, -0.022), (0.03, 0.026, 0.024)),
    # the trunk: a pear sitting up
    chest=((0, -0.018, 0.215), (0.064, 0.06, 0.07)),
    belly=((0, -0.006, 0.125), (0.078, 0.072, 0.09)),
    belly_top=0.27,
    # the hind legs: a haunch fused into each flank, a long foot forward
    haunch=((0.058, 0.018, 0.075), (0.046, 0.062, 0.062)),
    foot=((0.064, -0.048, 0.012), (0.024, 0.052, 0.013)),
    # the arms, forward and in to the nut under the chin
    arm=[(0.048, -0.032, 0.238), (0.05, -0.07, 0.205), (0.03, -0.096, 0.198)], arm_r=[0.02, 0.017, 0.015],
    hand=((0.026, -0.1, 0.196), (0.017, 0.016, 0.017)),
    nutc=(0, -0.115, 0.19), nutr=(0.03, 0.026, 0.034), hold_nut=True,
    # the ears: long, narrow, up and a little out
    ear_at=(0.42, 0.1, 0.86), ear_tip=(0.016, 0.006, 0.15), ear_w=0.021, ear_d=0.009, brush=0.05,
    # the tail: an S up behind, turned out toward +X, its top curling back and down
    tail=[(0, 0.055, 0.075), (0, 0.13, 0.09), (0, 0.18, 0.18), (0, 0.17, 0.3), (0, 0.19, 0.42), (0, 0.27, 0.465),
          (0, 0.325, 0.41), (0, 0.3, 0.345)],
    tail_r=[0.032, 0.062, 0.082, 0.088, 0.084, 0.073, 0.057, 0.038],
    tail_out=unit(0.5, 0.866, 0), tail_kind="fur",
    # the face
    eye_x=0.04, eye_dz=0.014, eye_half=(0.025, 0.029), eye_tilt=1.2, ring=1.32,
)


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


# --- body, legs, arms -------------------------------------------------------------------------------------------------
def add_body(k, P):
    for nm in ("chest", "belly"):
        c, r = P[nm]
        k.add(nm, ell(sc(P, c), sc(P, r)), P["coat"], "body")
    c, r = P["haunch"]
    k.add("haunch", ell(sc(P, c), sc(P, r)), P["coat"], "leg{s}", mirror=True)
    c, r = P["foot"]
    k.add("foot", ell(sc(P, c), sc(P, r)), P["coat"], "leg{s}", mirror=True)
    k.add("arm", tube([sc(P, p) for p in P["arm"]], np.array(P["arm_r"]) * P["s"]), P["coat"], "arm{s}", mirror=True)
    c, r = P["hand"]
    k.add("hand", ell(sc(P, c), sc(P, r)), P["coat"], "arm{s}", mirror=True)
    s = P["s"]
    # the cream belly: the front of the trunk, from the throat to between the legs
    bt = P["belly_top"] * s
    def belly(Q):
        w = 0.05 * s + 0.012 * s * np.clip((bt - Q[:, 2]) / (0.1 * s), 0, 1)
        return (inside(np.abs(Q[:, 0]) - w, 0.004) * inside(Q[:, 1] + 0.025 * s, 0.012) * inside(Q[:, 2] - bt, 0.006))
    k.paint(belly, P["cream"], on=["chest", "belly"])
    # toes: three dark grooves over each foot's front
    def toes(Q):
        fx = abs(P["foot"][0][0]) * s
        ax = np.abs(Q[:, 0]) - fx
        front = Q[:, 1] < (P["foot"][0][1] - P["foot"][1][1] * 0.55) * s
        return inside(np.abs(np.abs(ax) - 0.008 * s) - 0.002 * s, 0.001) * front
    k.paint(toes, P["swirl"], on=["foot"])


def add_nut(k, P):
    s = P["s"]
    c, r = sc(P, P["nutc"]), sc(P, P["nutr"])
    def acorn(V):
        V = V.copy(); V[:, 2] -= 0.18 * np.clip(-V[:, 2], 0, 1) ** 2 * 0; return V
    k.add("nut", outward(ell(c, r, rot=(-15, 0, 0), deform=acorn, sub=3)), P["nut"], "body", fuse=False, surface="shell")
    # the pale scar of its top and a dark cap line
    k.add("nuttop", outward(ell(c + np.array([0, 0.004 * s, r[2] * 0.72]), (r[0] * 0.62, r[1] * 0.62, r[2] * 0.32),
                                rot=(-15, 0, 0), sub=3)), P["nutcap"], "body", fuse=False, surface="shell")


# --- the head: cheeks, muzzle, nose, teeth, the cream rings round the eyes ----------------------------------------------
def add_head(k, P):
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    k.add("head", ell(H, HR), P["coat"], "head")
    o, r = P["cheek"]
    k.add("cheek", ell(H + sc(P, o), sc(P, r)), P["cream"], "head", mirror=True)
    o, r = P["muzzle"]
    k.add("muzzle", ell(H + sc(P, o), sc(P, r)), P["cream"], "head")
    k.add("neck", ell(H + sc(P, (0, 0.02, -0.07)), sc(P, (0.06, 0.055, 0.05))), P["coat"], "head")


def add_face(k, P):
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    o, r = P["muzzle"]
    mc, mr = H + sc(P, o), sc(P, r)
    eps = P["height"] * 0.004
    k.add("nose", outward(ell(mc + np.array([0, -mr[1] * 0.86, mr[2] * 0.55]), (0.011 * s, 0.008 * s, 0.007 * s),
                              sub=3)), P["nose"], "head", fuse=False, surface="skin")
    # two buck teeth under the nose
    for side in (1, -1):
        x = side * 0.0045 * s
        top = mc + np.array([x, -mr[1] * 0.88, -mr[2] * 0.35])
        k.add(f"tooth{'R' if side > 0 else 'L'}", hull([top + np.array([-0.004, 0, 0]) * s, top + np.array([0.004, 0, 0]) * s,
                                                         top + np.array([-0.004, 0.005, 0]) * s, top + np.array([0.004, 0.005, 0]) * s,
                                                         top + np.array([-0.0035, -0.001, -0.012]) * s,
                                                         top + np.array([0.0035, -0.001, -0.012]) * s,
                                                         top + np.array([-0.0035, 0.004, -0.012]) * s,
                                                         top + np.array([0.0035, 0.004, -0.012]) * s]),
              P["tooth"], "head", fuse=False, surface="shell")
    CC, CI = srgb(P["cream"]), srgb(P["ink"])
    def mouth(u, v):
        c = np.tile(CC, (len(u), 1))
        c[(np.abs(v - (-0.25 + 0.6 * u * u)) < 0.28) & (np.abs(u) < 0.85)] = CI
        return c
    k.decal(mc + np.array([0, -mr[1] * 0.9, -mr[2] * 0.3]), unit(0, -1, -0.1), (0.017 * s, 0.009 * s), mouth,
            eps=eps, rings=10, segs=44)
    # the cream rings round the eyes
    p, n = on_head(P["eye_x"] * s, H[2] + P["eye_dz"] * s, H, HR)
    n = unit(n[0] * P["eye_tilt"], n[1], n[2])
    hr = (P["eye_half"][0] * s * P["ring"], P["eye_half"][1] * s * P["ring"])
    k.decal(p, n, hr, P["cream"], eps=eps * 0.7, rings=10, segs=44, mirror=True)


# --- the ears: long and narrow, a pink lining, a dark brush at the tip ----------------------------------------------
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
        face = unit(side * 0.25, -1, 0.0); face = unit(*(face - d * (face @ d)))
        across = unit(*np.cross(d, face))
        def ring(t, ww, ff, off=0.0):
            c = root + d * L * t + face * off
            return [c + across * ww + face * ff, c - across * ww + face * ff, c + across * ww - face * ff,
                    c - across * ww - face * ff]
        def wid(t):
            return w * (1 - 0.1 * t - 0.75 * t ** 3) + 0.001
        k.add("ear" + nm, hull(ring(0.0, w * 0.9, dp) + ring(0.35, wid(0.35), dp) + ring(0.7, wid(0.7), dp * 0.85)
                              + [tp]), P["coat"], "ear" + nm, fuse=False, surface="fur")
        lp = []
        for t, f in ((0.08, 0.6), (0.4, 0.6), (0.7, 0.5)):
            lp += [root + d * L * t + across * wid(t) * f + face * dp * 1.15, root + d * L * t - across * wid(t) * f + face * dp * 1.15,
                   root + d * L * t + across * wid(t) * f * 0.8 + face * dp * 0.2, root + d * L * t - across * wid(t) * f * 0.8 + face * dp * 0.2]
        lp.append(root + d * L * 0.88 + face * dp * 0.8)
        k.add("earin" + nm, hull(lp), P["earin"], "ear" + nm, fuse=False, surface="fur")
        # the brush: thick dark points out of the tip, fanning up and out
        bl, bw = P["brush"] * s, 0.011 * s
        base = root + d * L * 0.72
        for i, (ang, ln) in enumerate(((0.0, 1.0), (0.42, 0.8), (-0.4, 0.78), (0.75, 0.55), (-0.7, 0.55))):
            way = unit(*(d * math.cos(ang) + across * math.sin(ang) * side - face * 0.15))
            k.add(f"brush{i}{nm}", spike(base + across * side * ang * 0.004 * s, way, bl * ln, bw, bw * 0.65, across=across),
                  P["tuft"], "ear" + nm, fuse=False, surface="fur")


# --- the tail ---------------------------------------------------------------------------------------------------------
def tail_pts(P):
    t = P["tail"]
    y0 = t[0][1]
    return [sc(P, np.array([0, y0, z]) + P["tail_out"] * (y - y0)) for x, y, z in t]


def chevrons(P, pts, colour_period=0.15, cover=0.3, start=0.06):
    """A paint mask of chevrons along a tube (arc length, the angle from its outer side): the swirls' stand-in."""
    s = P["s"]
    line = np.vstack([pts[i] + (pts[i + 1] - pts[i]) * f for i in range(len(pts) - 1)
                      for f in np.linspace(0, 1, 30, endpoint=False)])
    arc = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(line, axis=0), axis=1))])
    mid = line.mean(axis=0)
    def mask(Q):
        idx = np.argmin(((Q[:, None, :] - line[None, ::2]) ** 2).sum(2), axis=1) * 2
        idx = np.clip(idx, 0, len(line) - 2)
        c = line[idx]
        d = line[idx + 1] - line[idx]; d /= np.linalg.norm(d, axis=1, keepdims=True)
        outv = c - mid; outv -= d * (outv * d).sum(1, keepdims=True); outv /= np.linalg.norm(outv, axis=1, keepdims=True) + 1e-9
        r = Q - c; r -= d * (r * d).sum(1, keepdims=True); r /= np.linalg.norm(r, axis=1, keepdims=True) + 1e-9
        ang = np.arccos(np.clip((r * outv).sum(1), -1, 1))
        t = arc[idx]
        band = np.abs(tri(t / (colour_period * s) + ang * 0.28))
        return inside(band - cover, 0.06) * inside(start * s - t, 0.02)
    return mask


def add_tail(k, P):
    s = P["s"]
    pts = tail_pts(P)
    k.add("tail", tube(pts, np.array(P["tail_r"]) * s), P["tailc"], "tail")
    k.paint(chevrons(P, pts, cover=P.get("swirl_cover", 0.3)), P["swirl"], on=["tail"])
    if P["tail_kind"] != "fur":
        return
    # tufts of fur round the plume's outline, longest on its outer side, and a brush at the curl's end
    rng = np.random.RandomState(4)
    mid = np.mean(pts, axis=0)
    for i in range(18):
        t = 1.0 + (len(pts) - 2.2) * i / 17
        j = int(t); f = t - j
        c = pts[j] * (1 - f) + pts[j + 1] * f
        rr = (P["tail_r"][j] * (1 - f) + P["tail_r"][j + 1] * f) * s
        d = unit(*(pts[j + 1] - pts[j]))
        a, b = axes(d)
        th = rng.rand() * 2 * math.pi if i % 3 else 0.0
        o = c - mid; o -= d * (o @ d)
        o = unit(*o) if np.linalg.norm(o) > 1e-6 else a
        if i % 3:
            o = unit(*(o * 0.6 + (math.cos(th) * a + math.sin(th) * b) * 0.8))
        k.add(f"tailfur{i}", spike(c + o * rr * 0.75, unit(*(o + d * 0.7)), (0.05 + 0.018 * rng.rand()) * s,
                                   0.027 * s, 0.019 * s), P["tailc"] if i % 2 else P["swirl"], "tail", fuse=False,
              surface="fur")


# --- the eyes ---------------------------------------------------------------------------------------------------------
def eye_colour(P):
    CW, CI, CK, CG = srgb(P["white"]), srgb(P["iris"]), srgb(P["ink"]), srgb(P["white"])
    def eye(u, v):
        c = np.tile(CW, (len(u), 1))
        c[np.hypot((u + 0.06) / 0.74, (v + 0.04) / 0.8) < 1] = CI
        c[np.hypot((u + 0.06) / 0.38, (v + 0.04) / 0.42) < 1] = CK
        c[np.hypot((u - 0.2) / 0.24, (v - 0.28) / 0.24) < 1] = CG
        c[np.hypot((u + 0.3) / 0.1, (v + 0.35) / 0.1) < 1] = CG
        c[np.hypot(u, v) > 0.9] = CK
        return c
    return eye


def lay_eyes(k, P, state):
    s = P["s"]
    H, HR = sc(P, P["H"]), sc(P, P["HR"])
    p, n = on_head(P["eye_x"] * s, H[2] + P["eye_dz"] * s, H, HR)
    n = unit(n[0] * P["eye_tilt"], n[1], n[2])
    look = eye_look(state, eye_colour(P), srgb(P["cream"]), srgb(P["ink"]), line=0.16)
    k.decal(p, n, (P["eye_half"][0] * s, P["eye_half"][1] * s), look, eps=P["height"] * 0.0045, rings=20, segs=56,
            mirror=True)


def board(ob):
    me = ob.data
    co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    h = co[:, 2].max(); w = co[:, 0].max() - co[:, 0].min(); l = co[:, 1].max() - co[:, 1].min()
    square = 1.3 * max(h, 0.85 * max(w, l))
    corners = 0.853 * w + 0.522 * l
    print(f"KIT: board: height {h:.3f} width {w:.3f} length {l:.3f}; four-corner sum {corners:.3f} of a square "
          f"{square:.3f} ({corners / square:.2f})")


def squirrel(src, out_path, P, extra=None):
    k = Kit(src, coat="fur")
    add_body(k, P)
    add_head(k, P)
    add_ears(k, P)
    add_tail(k, P)
    if P["hold_nut"]:
        add_nut(k, P)
    if extra:
        extra(k, P)
    add_face(k, P)
    ob = k.build(voxel=P["voxel"], smooth=4, band=P["voxel"] * 0.8, wband=P["height"] * 0.06)
    board(ob)
    k.eye_states(lambda st: lay_eyes(k, P, st))
    k.export(out_path)


if __name__ == "__main__":
    squirrel(*args(), VEVEREL)
