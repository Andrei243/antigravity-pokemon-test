# Mega Dynamax Raichu X (a form of our own, plan 48): Mega Raichu X's own sculpt (raichumegax.py), whole and unchanged,
# with what the form adds laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/raichumegaxmmax.py -- <the export's Raichu-Mega-X-Mmax.glb> <out.glb>
#
# The design is one picture of the project's own (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Raichu-X.jpg). It
# draws a plain Raichu (orange, with a tail); taken from it is only what it adds over the Mega: the ears' upper halves
# grown into crystal, yellow at the root and cyan at the point; the bolt at the end of the tail turned to glowing crystal,
# which on the Mega (it has no tail) is its two great trails of lightning, their yellow glowing and their orange points
# turned to cyan glass; sparks of yellow lightning about it; the Key Stone's emblem on the belly (a cyan ring, a pale
# disc, a pink star); clouds of Max energy rolling on the ground under it and a wisp over each shoulder.
#
# Also the helpers the four forms of this batch share (raichumegaymmax.py, alakazammegammax.py, gengarmegammax.py import
# this script through sibling): the colours, join, star_outline, emblem, clouds, puffs, zig.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, ring_band, axes, srgb, unit, sibling, args

# The clouds' colours, the sculptor's (PokemonModels.MegaMax1.cs): crimson, deep crimson, violet, cyan
CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"


def join(shapes):
    """Several shapes as one (one material for a glowing colour, one primitive)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float))
        F.extend([[i + off for i in t] for t in f])
        off += len(v)
    return np.vstack(V), F


def star_outline(R, r, n=4, phase=math.pi / 2):
    """A star of n points: tips at R, the notches between them at r."""
    pts = []
    for i in range(2 * n):
        a = phase + math.pi * i / n
        rr = R if i % 2 == 0 else r
        pts.append((rr * math.cos(a), rr * math.sin(a)))
    return pts


def emblem(k, c, d, r, ring, disc, star, bone, name="mxemblem"):
    """
    The Key Stone's emblem, loose and glowing: a low dome of a disc (c on the body's surface, d out of it; its rim
    pulled back so that it hugs a curved chest), a ring round it, and a four-pointed star standing on it.
    """
    d = unit(*d)
    c = np.array(c, float)
    k.add(name + "disc", lathe(c, d, [(0, -0.25 * r), (0.97 * r, -0.25 * r), (r, -0.12 * r), (0.86 * r, 0.03 * r),
                                      (0.5 * r, 0.12 * r), (0, 0.15 * r)]), disc, bone, fuse=False, glow=disc)
    k.add(name + "ring", ring_band(c - d * 0.04 * r, d, 1.06 * r, 0.26 * r, thick=0.17 * r), ring, bone, fuse=False,
          glow=ring)
    a, b = axes(d)
    k.add(name + "star", slab(star_outline(0.86 * r, 0.24 * r), c + d * 0.2 * r, a, b, 0.1 * r), star, bone,
          fuse=False, glow=star)


def _ones(P):
    return np.ones(len(P))


def clouds(k, cx, cy, rx, ry, n, s, seed, bone="root", rider=True):
    """
    The clouds of Max energy on the ground: a ring of n fused mounds round (cx, cy), each three round puffs of mixed
    sizes and one riding on top, in crimson, violet and deep crimson by turns with a puff of another colour, and a cyan
    curl glowing on every third. Fused, each an island of its own in the remesh; named "mxc<colour>" and painted back
    to their own colours after the Mega's paints (a paint of the Mega's with no `on` would take them otherwise).
    """
    rnd = np.random.default_rng(seed)
    cols = [("C", CRIMSON), ("V", VIOLET), ("D", DEEP)]
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + rnd.uniform(-0.12, 0.12)
        rr = 1 + 0.05 * math.sin(3 * a)
        base = np.array([cx + rx * rr * math.cos(a), cy + ry * rr * math.sin(a)])
        tang = unit(-rx * math.sin(a), ry * math.cos(a), 0)[:2]
        rad = unit(ry * math.cos(a), rx * math.sin(a), 0)[:2]
        lay = [(1.0, 0.0, 0.0), (0.74, 1.05, 0.25), (0.62, -1.0, -0.2), (0.5, 0.25, 0.1)]
        for j, (f, ot, orr) in enumerate(lay if rider else lay[:3]):
            sj = s * f * (1 + 0.12 * rnd.uniform(-1, 1))
            p = base + tang * ot * s + rad * orr * s
            z = sj * 0.85 if j < 3 else s * 1.3
            letter, col = cols[(i + (1 if j == 2 else 0)) % 3]
            k.add(f"mxc{letter}{i}_{j}", ell((p[0], p[1], z), (sj, sj, sj * 0.85)), col, bone)
        if i % 3 == 0:
            q = base + tang * 0.5 * s
            curls.append(ell((q[0], q[1], s * 1.55), (0.3 * s, 0.3 * s, 0.24 * s), sub=3))
    for letter, col in cols:
        k.paint(_ones, col, on=["mxc" + letter])
    if curls:
        k.add("mxcurls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def puffs(k, name, pts, sizes, bone, mirror=False, colours=(CRIMSON, VIOLET, DEEP)):
    """A string of loose round puffs (a wisp, a ring floating over the head), one piece a colour."""
    by = {}
    for i, (p, sz) in enumerate(zip(pts, sizes)):
        by.setdefault(colours[i % len(colours)], []).append(ell(p, (sz, sz, sz * 0.86), sub=3))
    for j, (col, shapes) in enumerate(by.items()):
        k.add(f"{name}{j}", join(shapes), col, bone, fuse=False, mirror=mirror)


def zig(a, b, jag, n=5, side=None):
    """Points of a jagged bolt from a to b, jag across."""
    a, b = np.array(a, float), np.array(b, float)
    d = b - a
    if side is None:
        side = np.cross(d, (0, 0, 1.0))
        if np.linalg.norm(side) < 1e-6:
            side = np.array([1.0, 0, 0])
    side = np.array(side, float) / np.linalg.norm(side)
    return [a + d * (i / n) + side * (jag * (1 if i % 2 else -1) if 0 < i < n else 0) for i in range(n + 1)]


def kite(p, e, w, t):
    """The four corners of an ear's section about p: across the ear (in the plane of e and z) by w, front to back by t."""
    across = unit(*np.cross(e, (0, 1.0, 0)))
    y = np.array([0, 1.0, 0])
    return [p + across * w, p - across * w, p + y * t, p - y * t]


# --- Mega Dynamax Raichu X -------------------------------------------------------------------------------------------

GLASSY, GLASSC, BOLTGLOW, SPARK = "#FFE65C", "#7CEEF4", "#FFE04A", "#FFE860"


def maxed_raichu_x(k):
    # The ear's centre line and radii, from raichumegax.py's raichu_mega_x (EAR, ERS; a blade flattened to 0.45 front
    # to back): the crystal sheathes the upper half (the black reaches about z 1.27) and ends at the ear's own point
    EAR = np.array([(0.065, 0.01, 1.07), (0.098, 0.012, 1.125), (0.124, 0.014, 1.21), (0.166, 0.016, 1.32),
                    (0.236, 0.016, 1.41), (0.33, 0.016, 1.472)])
    ERS = [0.044, 0.06, 0.064, 0.05, 0.028, 0.007]
    def at(t):
        i = min(int(t), 4); f = t - i
        return EAR[i] + (EAR[i + 1] - EAR[i]) * f, ERS[i] + (ERS[i + 1] - ERS[i]) * f
    def sec(t):
        p, r = at(t)
        e = unit(*(at(min(t + 0.2, 5))[0] - at(max(t - 0.2, 0))[0]))
        return kite(p, e, r * 1.12 + 0.006, r * 0.45 * 1.2 + 0.007)
    yellow = hull(sec(2.62) + sec(3.0) + sec(3.5))
    tipp = EAR[5] + unit(*(EAR[5] - EAR[4])) * 0.006
    cyan = [hull(sec(3.5) + sec(4.0) + sec(4.5) + [tipp])]
    # two shards jutting from the ear's outer edge, as the picture's jagged crystal
    for t, ln, w in ((3.0, 0.075, 0.017), (4.0, 0.06, 0.015)):
        p, r = at(t)
        e = unit(*(at(t + 0.2)[0] - at(t - 0.2)[0]))
        out_ = -unit(*np.cross(e, (0, 1.0, 0)))
        base = p + out_ * r * 0.8
        tip = base + unit(*(out_ * 0.55 + e)) * ln
        cyan.append(hull(kite(base, e, w, 0.016) + [tip]))
    k.add("mxearglass", yellow, GLASSY, "ear{s}", fuse=False, glow=GLASSY, mirror=True)
    k.add("mxearshard", join(cyan), GLASSC, "ear{s}", fuse=False, glow=GLASSC, mirror=True)

    # The trails of lightning turned to crystal: their yellow glowing, their orange points cyan glass (the Mega's own
    # pieces "bolt0R/L" and "bolt1R/L"; only colour and glow change, never their shape)
    for p in k.parts:
        if p["name"].startswith("bolt0"):
            p["glow"] = BOLTGLOW
        elif p["name"].startswith("bolt1"):
            p["color"] = srgb(GLASSC)
            p["glow"] = GLASSC

    # Sparks of lightning: low beside the legs and behind the head
    sparks = [tube(zig((0.25, 0.03, 0.73), (0.35, 0.02, 0.6), 0.02, side=(0, 0.3, 1)), [0.016] * 6, seg=10),
              tube(zig((-0.24, 0.04, 0.75), (-0.34, 0.03, 0.63), 0.02, side=(0, 0.3, 1)), [0.016] * 6, seg=10),
              tube(zig((-0.12, 0.17, 0.99), (-0.21, 0.21, 1.08), 0.018, side=(0, 0, 1)), [0.016] * 6, seg=10)]
    k.add("mxspark", join(sparks), SPARK, "body", fuse=False, glow=SPARK)

    # The emblem on the white belly (the body's pear stands at y -0.109 there)
    emblem(k, (0, -0.105, 0.79), (0, -1, 0.03), 0.062, "#5EE0E8", "#FFF4DC", "#FF86C0", "body")

    # The clouds on the ground under it, round the trails' points (they reach the ground at x 0.52, y 0.17), and a wisp
    # rising behind each shoulder
    clouds(k, 0.0, 0.04, 0.5, 0.25, 6, 0.072, 11, rider=False)
    wisp = [(0.3, 0.08, 0.94), (0.355, 0.11, 0.99), (0.395, 0.13, 1.045)]
    puffs(k, "mxwisp", wisp, [0.05, 0.045, 0.034], "body", mirror=True)


if __name__ == "__main__":
    _build, _laid = Kit.build, []
    def build(self, *a, **kw):
        if not _laid:               # the first build is the body's; the eyes' five states build after it
            _laid.append(True)
            maxed_raichu_x(self)
        return _build(self, *a, **kw)
    Kit.build = build
    m = sibling("raichumegax")
    m.raichu_mega_x(*args())
