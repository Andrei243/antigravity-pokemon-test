# Great Tusk sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/greattusk.py -- <the export's Great Tusk.glb> <out.glb>
#
# Donphan's ancient past, a design of its own (Phanpy's stub legs and on_ell are borrowed, nothing else). From the
# references: a heavy pale blue-grey barrel of a body, low, its belly near the ground; a dark violet plate of armour
# over the whole back and the crown, its hem jagged down the flanks, rows of pink-red spikes along its ridge and down
# its sides; the plate runs on over the brow and down in front of the face as a broad, thick band of segments to the
# ground, curling back under at its foot (the wheel it rolls on), pink spikes along its edges; two huge cream tusks
# out of the face low beside the band, forward and out, then up and curling back in, each a great C as tall as the
# body; a narrow dark violet ear out of each side of the head under the plate, pointing forward, red underneath;
# a small slanted yellow eye ringed red under the plate's brow; a jagged white grin on each cheek; a cluster of red
# diamonds on each flank under the hem; short thick legs hidden in ruffs of spiky pink-red fur, cream claws; a long
# tail trailing back and a little out, pale below and dark violet above with plates along its top.
# Written to be grown from (irontreads.py imports it with sibling("greattusk")): the band, the spikes and the eye are
# functions with their sizes and colours as arguments; the build is under the main guard.
# Bones: body (0, 0.02, 0.32), head (0, -0.22, 0.32), ears (+-0.09, -0.24, 0.34), legs (+-0.13, -0.14, 0.24), hind legs
# (+-0.13, 0.17, 0.24), tail (0, 0.29, 0.36).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, blade, hull, slab, sheet, outward, polygon, srgb, inside, unit, eye_look, args,
                       sibling, tri)

p = sibling("phanpy")
on_ell = p.on_ell

GREY, BELLY = "#A9B8D8", "#9CABCC"
ARMOUR, GROOVE = "#4E4282", "#2C2448"
SPIKE, SPIKE_DARK = "#E43C6C", "#B42850"
RED_SPOT = "#C8203A"
TUSK, CLAW = "#E9E0C6", "#F1EADA"
FUR, FUR_DARK = "#E44C78", "#B02E54"
INK, YELLOW, EYE_RED, WHITE = "#16141E", "#F4D23C", "#D83A5C", "#FFFFFF"

H = np.array([0.0, -0.23, 0.31])               # the head's middle
HR = np.array([0.15, 0.13, 0.14])


# --- a smooth line and its points evenly spaced ----------------------------------------------------------------------
def path(pts, n):
    """n points evenly along a smooth line through pts (y, z) in the midline, with their tangents and its length."""
    P = np.array([(0.0, y, z) for y, z in pts])
    ext = np.vstack([2 * P[0] - P[1], P, 2 * P[-1] - P[-2]])
    fine = []
    for i in range(len(P) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for t in np.linspace(0, 1, 40, endpoint=False):
            fine.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                               + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    fine.append(P[-1])
    fine = np.array(fine)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(fine, axis=0), axis=1))])
    at = np.linspace(0, s[-1], n)
    Q = np.stack([np.interp(at, s, fine[:, j]) for j in range(3)], axis=1)
    T = np.gradient(Q, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return Q, T, s[-1]


# --- a spike: a flat-faced solid from a base on a surface out along a direction ----------------------------------------
def spike(k, name, at, n, d, length, base, col, bone, flat=0.55, surface="shell"):
    """A kite-sectioned spike: its base a diamond about `at` (sunk a little under the surface of normal n), its tip
    `length` along d. flat: its thickness across against its width along d's side."""
    n = unit(*n); d = unit(*d)
    side = np.cross(d, n)
    if np.linalg.norm(side) < 1e-3:
        side = np.cross(d, (1.0, 0, 0))
    side /= np.linalg.norm(side)
    up = np.cross(side, d)
    a = np.array(at, float) - n * base * 0.5
    pts = [a + side * base, a - side * base, a + up * base * flat, a - up * base * flat,
           a - d * base * 0.6, a + d * length]
    k.add(name, hull(pts), col, bone, fuse=False, surface=surface)


# --- the band of segments: armour from the brow (or the back) down in front of the face -------------------------------
def band(k, line, n, width, depth, block, groove, bone, strips=3, core=0.75, core_from=0.0, extra=None, surface="shell"):
    """
    A band of n blocks along line (y, z points in the midline): a dark fused core along it (core_from 0..1 of the way
    on) and over it a flat-faced block a step, each cut into strips across so it can curve. width(f), depth(f) give its
    half width and thickness at f (0..1 along). bone(f) names a block's bone. extra(k, i, c, t, N, X, w, d, half) adds a
    block's own pieces (spikes, panels), with its middle c on the line, its tangent t, its outward normal N.
    """
    Q, T, L = path(line, 120)
    f = np.linspace(0, 1, len(Q))
    w, d = width(f), depth(f)
    X = np.array([1.0, 0, 0])
    sel = f >= core_from
    k.add("bandcore", tube(Q[sel][:-2:5], d[sel][:-2:5] * core), groove, bone(0.9), surface=surface)
    for i in range(n):
        g = (i + 0.5) / n
        j = int(round(g * (len(Q) - 1)))
        c, t = Q[j], T[j]
        N = np.cross(t, X); N /= np.linalg.norm(N)
        if N @ (c - np.array([0, 0.0, 0.3])) < 0:
            N = -N
        hl = L / n * 0.47
        wi, di = float(width(np.array([g]))[0]), float(depth(np.array([g]))[0])
        ch = min(0.016, hl * 0.35)
        cuts = np.linspace(-wi, wi, strips + 1)
        for q in range(strips):
            b0, b1 = cuts[q] - 0.002, cuts[q + 1] + 0.002
            pts = []
            for b in (b0, b1):
                drop = -0.25 * di * (b / wi) ** 2
                edge = ch if abs(b) > wi - 0.01 else 0.0
                for a in (-hl, hl):
                    pts.append(c + t * a + X * b + N * (drop - di * 0.55))
                    pts.append(c + t * a + X * b + N * (drop + di - ch - edge * 0.5))
                for a in (-hl + ch, hl - ch):
                    pts.append(c + t * a + X * b + N * (drop + di - edge))
            k.add(f"band{i}_{q}", hull(pts), block, bone(g), fuse=False, surface=surface)
        if extra:
            extra(k, i, c, t, N, X, wi, di, hl, bone(g))


# --- the plate over the back: a thick sheet with a jagged hem ---------------------------------------------------------
PLATE_Y = np.array([-0.31, -0.22, -0.10, 0.05, 0.20, 0.34])
PLATE_RIDGE = np.array([0.49, 0.535, 0.56, 0.565, 0.535, 0.43])
PLATE_HALF = np.array([0.155, 0.195, 0.228, 0.238, 0.228, 0.175])
PLATE_HEM = np.array([0.36, 0.31, 0.275, 0.265, 0.275, 0.315])
TEETH = 6                                         # the hem's points down each flank


def plate_at(u, t):
    """A point of the plate's middle surface: u 0..1 front to back, t the angle from the ridge (+ to +X)."""
    y = PLATE_Y[0] + (PLATE_Y[-1] - PLATE_Y[0]) * u
    zr = np.interp(y, PLATE_Y, PLATE_RIDGE)
    a = np.interp(y, PLATE_Y, PLATE_HALF)
    hem = np.interp(y, PLATE_Y, PLATE_HEM)
    zc = (hem + 0.323 * zr) / 1.323                  # the hem lies at t = 1.9 rad
    return np.stack([a * np.sin(t), y, zc + (zr - zc) * np.cos(t)], axis=-1)


def plate_reach(u):
    """How far down a flank the plate reaches at u: a row of points (a jagged hem)."""
    return 1.72 + 0.26 * (1 - np.abs(tri(u * TEETH + 0.5)))


def plate(k, col=ARMOUR, thick=0.026):
    def at(u, v):
        uu = 0.004 + 0.992 * u
        t = (v * 2 - 1) * plate_reach(uu)
        return plate_at(uu, t)
    k.add("plate", outward(sheet(at, 90, 61, thick)), col, "body", fuse=False, surface="shell")


def plate_point(u, t, lift=0.013):
    """A point on the plate's outer face and its normal there."""
    e = 1e-3
    c = plate_at(np.array(u), np.array(t))
    du = (plate_at(np.array(u + e), np.array(t)) - plate_at(np.array(u - e), np.array(t)))
    dt = (plate_at(np.array(u), np.array(t + e)) - plate_at(np.array(u), np.array(t - e)))
    nn = np.cross(du, dt); nn /= np.linalg.norm(nn)
    if nn[2] < -0.5 or (nn @ (c - np.array([0, c[1], 0.33]))) < 0:
        nn = -nn
    return c + nn * lift, nn


def plate_spikes(k):
    """Rows of pink spikes: the ridge's, tall, leaning back; two rows down each side, shorter."""
    for i, u in enumerate(np.linspace(0.1, 0.88, 6)):
        c, nn = plate_point(u, 0.0, 0.006)
        L = 0.11 - 0.04 * abs(u - 0.45)
        spike(k, f"ridge{i}", c, nn, nn * 0.75 + np.array([0, 0.75, 0]), L, 0.042, SPIKE, "body")
    for row, (t0, n_, L, b) in enumerate(((0.75, 5, 0.085, 0.036), (1.38, 5, 0.065, 0.03))):
        for i, u in enumerate(np.linspace(0.14 + 0.05 * row, 0.86, n_)):
            for s in (1, -1):
                c, nn = plate_point(u, s * t0, 0.006)
                col = SPIKE if (i + row) % 3 else SPIKE_DARK
                spike(k, f"side{row}_{i}{'R' if s > 0 else 'L'}", c, nn, nn * 0.8 + np.array([0, 0.6, 0.15]), L, b,
                      col, "body")


# --- the eye: a small slanted yellow almond with a slit, ringed red ----------------------------------------------------
def slit_eye(yellow=YELLOW, ring=EYE_RED, ink=INK):
    Y, Rg, K, Wt = srgb(yellow), srgb(ring), srgb(ink), srgb(WHITE)
    def look(u, v):
        c = np.tile(Rg, (len(u), 1))
        r = np.hypot(u / 0.8, v / 0.72)
        c[r < 1] = Y
        c[(np.abs(u + 0.05) < 0.17) & (np.abs(v) < 0.62)] = K
        c[np.hypot((u - 0.32) / 0.16, (v - 0.28) / 0.16) < 1] = Wt
        return c
    return look


def almond(th):
    """An almond: pointed at both ends."""
    return 0.62 + 0.38 * abs(math.cos(th)) ** 0.5 if abs(math.sin(th)) > 0 else 1.0


def eyes(k, centre, normal, half, lid, look=None, up=(0, 0.25, 1)):
    look = look or slit_eye()
    L, K = srgb(lid), srgb(INK)
    def lay(state):
        if state == "Fierce":
            def f(u, v, look=look):
                c = look(u, v)
                edge = 0.18 + 0.45 * u
                c[v > edge] = L
                c[np.abs(v - edge) < 0.16] = K
                return c
        else:
            f = eye_look(state, look, L, K, line=0.2)
        k.decal(centre, normal, half, f, eps=0.003, rings=20, segs=56, mirror=True, up=up)
    return lay


# --- Great Tusk ------------------------------------------------------------------------------------------------------
TRUNK = [(-0.27, 0.47), (-0.36, 0.455), (-0.435, 0.39), (-0.475, 0.29), (-0.49, 0.19), (-0.475, 0.1),
         (-0.435, 0.065), (-0.38, 0.065)]
TUSK_PATH = [(0.1, -0.32, 0.2), (0.16, -0.42, 0.14), (0.22, -0.53, 0.15), (0.258, -0.62, 0.23), (0.265, -0.645, 0.34),
             (0.242, -0.605, 0.44), (0.197, -0.525, 0.48), (0.158, -0.455, 0.47)]
TUSK_R = [0.042, 0.04, 0.036, 0.031, 0.026, 0.02, 0.014, 0.008]


def trunk_pads(k, i, c, t, N, X, w, d, hl, bone):
    """A pink spike on each edge of a block, out and back along the band."""
    for s in (1, -1):
        at = c + X * s * (w - 0.004) + N * d * 0.3
        out = X * s * 0.8 + N * 0.35 - t * 0.35
        spike(k, f"pad{i}{'R' if s > 0 else 'L'}", at, X * s, out, 0.045, 0.022, SPIKE, bone)


def ears(k):
    """A narrow dark violet ear out of each side of the head under the plate, pointing forward and out, red under."""
    o = np.array([0.125, -0.25, 0.37])
    ex = unit(0.45, -0.88, -0.12)
    ez = unit(0.0, 0.1, 1.0)
    out = [(0.0, -0.038), (0.07, -0.036), (0.15, -0.02), (0.215, 0.0), (0.15, 0.014), (0.07, 0.03), (0.0, 0.036)]
    k.add("ear", slab(out, o, ex, ez, 0.024), ARMOUR, "ear{s}", fuse=False, mirror=True, surface="shell")
    red = [(0.02, -0.03), (0.07, -0.029), (0.15, -0.016), (0.19, -0.004), (0.15, 0.0), (0.07, -0.004), (0.02, -0.004)]
    k.add("earred", slab(red, o - np.array([0, 0, 0.004]), ex, ez, 0.028), SPIKE, "ear{s}", fuse=False, mirror=True,
          surface="skin")


def fur_ruffs(k, x, ys):
    """A ruff of spiky pink-red fur round each ankle: shards out and down, two pinks."""
    for y, bone in ((ys[0], "leg{s}"), (ys[1], "hind{s}")):
        for tier, (z, m, L, b, rr) in enumerate(((0.13, 8, 0.075, 0.036, 0.066), (0.075, 9, 0.065, 0.032, 0.072))):
            for j in range(m):
                a = 2 * math.pi * (j + 0.5 * tier) / m + 0.2
                r = np.array([math.cos(a), math.sin(a), 0.0])
                at = np.array([x, y, z]) + r * rr
                d = r * 0.8 + np.array([0, 0, -0.6])
                col = FUR if (j + tier) % 3 else FUR_DARK
                spike(k, f"fur{y:+.2f}_{tier}{j}", at, r, d, L, b, col, bone, flat=0.6, surface="fur")
    def lower_leg(P):
        return inside(P[:, 2] - 0.11, 0.003)
    k.paint(lower_leg, FUR, on=["leg", "hind"])


def mirror_last(k, n):
    """Copy the last n loose parts to -X (their bone's R to L)."""
    for part in list(k.parts[-n:]):
        k.parts.append(dict(part, name=part["name"] + "L", V=part["V"] * np.array([-1, 1, 1]),
                            F=[f[::-1] for f in part["F"]], bone=part["bone"].replace("R", "L")))


def great_tusk(src, out_path):
    k = Kit(src, coat="shell")
    def barrel(V):
        V = V.copy()
        V[:, :2] *= (1 + 0.1 * np.clip(-V[:, 2], 0, 1))[:, None]
        return V
    k.add("body", ell((0, 0.04, 0.29), (0.2, 0.27, 0.205), deform=barrel), GREY, "body")
    k.add("back", ell((0, 0.04, 0.37), (0.19, 0.27, 0.165)), GREY, "body")
    k.add("rump", ell((0, 0.19, 0.27), (0.19, 0.15, 0.16)), GREY, "body")
    k.add("chest", ell((0, -0.13, 0.27), (0.18, 0.14, 0.16)), GREY, "body")
    k.add("head", ell(H, HR), GREY, "head")
    k.add("jaw", ell((0, -0.3, 0.21), (0.12, 0.085, 0.08)), GREY, "head")
    # the plate and its spikes
    plate(k)
    plate_spikes(k)
    # the band over the brow and down to the ground
    band(k, TRUNK, 9, lambda f: np.interp(f, [0, 0.3, 1], [0.1, 0.098, 0.086]),
         lambda f: np.interp(f, [0, 0.4, 1], [0.055, 0.06, 0.055]), ARMOUR, GROOVE,
         lambda f: "head", extra=trunk_pads)
    ears(k)
    # the tusks
    k.add("tusk", tube(TUSK_PATH, TUSK_R, seg=20), TUSK, "head", fuse=False, mirror=True, surface="shell")
    # legs, claws and ruffs of fur
    p.legs(k, 0.13, -0.14, 0.17, 0.17, (0.1, 0.1, 0.1), 0.07, (0.08, 0.088, 0.032), GREY, CLAW, nails=3,
           nail_size=0.028)
    n0 = len(k.parts)
    fur_ruffs(k, 0.13, (-0.15, 0.16))
    shards = [q for q in k.parts[n0:] if q["name"].startswith("fur")]
    for q in shards:
        q["bone"] = q["bone"].format(s="R")
        k.parts.append(dict(q, name=q["name"] + "L", V=q["V"] * np.array([-1, 1, 1]), F=[f[::-1] for f in q["F"]],
                            bone=q["bone"].replace("R", "L")))
    # the tail: long, back and a little out, dark violet plates along its top
    TAIL = [(0, 0.27, 0.34), (0.02, 0.37, 0.28), (0.06, 0.46, 0.22), (0.11, 0.53, 0.18), (0.15, 0.58, 0.17)]
    k.add("tail", tube(TAIL, [0.072, 0.056, 0.04, 0.026, 0.013]), GREY, "tail")
    TP = np.array(TAIL)
    def tail_top(P):
        z_line = np.interp(P[:, 1], TP[:, 1], TP[:, 2])
        return inside(z_line + 0.006 - P[:, 2], 0.003) * (P[:, 1] > 0.3)
    k.paint(tail_top, ARMOUR, on=["tail"])
    for i, g in enumerate((0.2, 0.38, 0.56, 0.74)):
        Q, T, L = path([(y, z) for _, y, z in TAIL], 60)
        j = int(g * 59)
        x = np.interp(Q[j][1], TP[:, 1], TP[:, 0])
        r = np.interp(Q[j][1], TP[:, 1], [0.072, 0.056, 0.04, 0.026, 0.013])
        c = np.array([x, Q[j][1], Q[j][2] + r * 0.85])
        spike(k, f"tailplate{i}", c, (0, -0.2, 1), (0.1, 0.7, 0.75), 0.06 - 0.008 * i, 0.03 - 0.004 * i, ARMOUR,
              "tail")
    # red diamonds on the flanks, under the hem
    diamond = polygon([(1, 0), (0, 1), (-1, 0), (0, -1)])
    for (y, z, s) in ((0.09, 0.205, 0.036), (0.15, 0.235, 0.023), (0.155, 0.18, 0.021), (0.05, 0.165, 0.02)):
        d = unit(1.0, (y - 0.04) / 0.27, (z - 0.29) / 0.205 * 0.9)
        c = on_ell((0, 0.04, 0.29), (0.2 * (1 + 0.1 * max(0, 0.29 - z) / 0.205), 0.27, 0.205), d)
        k.decal(c, d, (s, s), RED_SPOT, eps=0.003, outline=diamond, rings=8, segs=40, mirror=True)
    # a jagged white grin on each cheek
    Wt, K, G = srgb(WHITE), srgb(INK), srgb(GREY)
    def grin(u, v):
        c = np.tile(G, (len(u), 1))
        zig = 0.42 * tri((u + 1) * 1.25)
        band_ = np.abs(v - zig) < 0.62
        c[band_] = K
        c[np.abs(v - zig) < 0.32] = Wt
        return c
    gd = unit(0.74, -0.55, -0.38)
    k.decal(on_ell(H, HR, gd), gd, (0.05, 0.022), grin, eps=0.003, rings=12, segs=56, mirror=True, up=(0, 0.3, 1))
    k.build(voxel=0.0065, smooth=5, band=0.006)
    ed = unit(0.82, -0.5, 0.12)
    k.eye_states(eyes(k, on_ell(H, HR, ed), ed, (0.03, 0.021), GREY))
    k.export(out_path)


if __name__ == "__main__":
    great_tusk(*args())
