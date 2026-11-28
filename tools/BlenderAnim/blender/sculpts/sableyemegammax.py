# Mega Dynamax Sableye (plan 48), a form of our own: Mega Sableye's own sculpt (sableyemega.py) whole and unchanged,
# with what the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Sableye.jpg) lays over it, laid on the
# Mega's kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/sableyemegammax.py -- <the export's Sableye-Mega-Mmax.glb> <out.glb>
# What the picture adds, largest first:
#   1. the shield grown into a great cut gem taller than the imp: a crystal of the Mega's own hexagonal cut, a third
#      again as large as the Mega's shield (which stays inside it), grown out and up away from the hand that grips it,
#      red bevels round a violet table, both glowing;
#   2. crystals of red and violet standing round its top and its sides;
#   3. the Key Stone's emblem on the shield's table: a glowing disc in a glowing ring with the stone's star in it;
#   4. the clouds of Max energy about the feet and the shield's foot, and a wisp at the +X side.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, slab, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
GEM_RED, GEM_RED_GLOW = "#C8243E", "#B81E3C"
TABLE, TABLE_GLOW = "#8E44D8", "#9A4AE0"
CRYS_RED, CRYS_VIOLET = "#DA3452", "#9C4ADA"
STONE, RING, STAR = "#F6CCFF", "#FF7AC8", "#4A1A78"


def clouds(k, centre, rx, ry, n, size, avoid, seed, phase=0.0):
    """A broken ring of fused mounds on the ground: clumps of round puffs, crimson and violet by turns, a few cyan
    curls riding on them; a clump that would come near what is in avoid ((x, y), r) is left out."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    i = 0
    for m in range(n):
        a = phase + 2 * math.pi * (m + 0.5) / n
        c = np.array([centre[0] + rx * math.cos(a), centre[1] + ry * math.sin(a)])
        if any(np.hypot(*(c - np.array(p))) < r + size * 1.5 for p, r in avoid):
            continue
        col = cols[i % 4]
        for j in range(4):
            s = size * (1.0, 0.75, 0.62, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.32, -0.3, 0.1)[j] * size / 0.08 * 0.5
            rr = (0.0, 0.03, 0.02, -0.04)[j] * size / 0.08
            p = np.array([centre[0] + (rx + rr) * math.cos(t), centre[1] + (ry + rr) * math.sin(t),
                          s * (0.8, 0.9, 0.85, 1.9)[j]])
            pc = col if j != 2 else cols[(i + 1) % 4]
            name = f"mxc{i:02d}{j}"
            k.add(name, ell(p, (s, s, s * 0.9)), pc, "root")
            k.paint(lambda P: np.ones(len(P)), pc, on=[name])
        if i % 3 == 0:
            q = np.array([centre[0] + (rx + 0.02) * math.cos(a), centre[1] + (ry + 0.02) * math.sin(a), size * 1.25])
            k.add(f"mxcurl{i:02d}", ell(q, (size * 0.32, size * 0.32, size * 0.28)), CYAN, "root", fuse=False,
                  glow=CYAN)
        i += 1


def wisp(k, r, a0, a1, z0, z1, s0, s1, col, bone, tag, n=14):
    """A strand of Max energy swept round the body on an arc about its upright axis (angles in degrees from +X
    toward the back), rising or falling as it goes: a smooth fused tube thinning toward its end, with two round
    puffs swelling at its root."""
    pts, rad = [], []
    for t in np.linspace(0, 1, n):
        a = math.radians(a0 + (a1 - a0) * t)
        pts.append((r * math.cos(a), r * math.sin(a), z0 + (z1 - z0) * t))
        rad.append(s0 + (s1 - s0) * t)
    k.add(f"mxw{tag}t", tube(pts, rad), col, bone)
    for j, (i, f) in enumerate(((0, 1.3), (n // 3, 1.12))):
        k.add(f"mxw{tag}p{j}", ell(pts[i], (rad[i] * f, rad[i] * f, rad[i] * f * 0.9)), col, bone)
    k.paint(lambda P: np.ones(len(P)), col, on=[f"mxw{tag}"])


def prism(base, d, length, r):
    """A crystal: a six-sided prism with a pointed end."""
    return lathe(base, d, [(0, 0), (r, 0), (r, length * 0.72), (0, length)], seg=6)


def star(n4=4, r=1.0, inner=0.32):
    pts = []
    for i in range(2 * n4):
        a = math.pi / 2 + i * math.pi / n4
        rr = r if i % 2 == 0 else r * inner
        pts.append((rr * math.cos(a), rr * math.sin(a)))
    return pts


def maxed(k, m):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    SN, SA, SR = m.SN, m.SA, m.SR
    # 1. the great gem: the Mega's cut, grown out (-X) and up from the gripped inner edge, the Mega's shield inside it
    SC = m.SC + SA * 0.06 - SR * 0.02
    SW, SH = 0.2, 0.36
    pts = []
    for a, b in m.HEXAGON:
        e = SR * a * SW + SA * b * SH
        pts.append(SC + e)
        pts.append(SC + SN * 0.062 + e * 0.6)
        pts.append(SC - SN * 0.05 + e * 0.82)
    k.add("mxgem", hull(pts), GEM_RED, "armL", fuse=False, glow=GEM_RED_GLOW, surface="shell")
    table = [(a * SW * 0.6, b * SH * 0.6) for a, b in m.HEXAGON]
    k.add("mxgemtable", slab(table, SC + SN * 0.064, SR, SA, 0.006), TABLE, "armL", fuse=False, glow=TABLE_GLOW,
          surface="shell")
    # 2. crystals round its top and its sides, red and violet by turns
    rnd = np.random.default_rng(11)
    corners = [SC + SR * a * SW + SA * b * SH for a, b in m.HEXAGON]
    n = 0
    for e0, e1 in zip(corners, corners[1:] + corners[:1]):
        for t in np.linspace(0.1, 0.9, 4):
            p = e0 + (e1 - e0) * t
            off = p - SC
            if off @ SA < -0.12:                                   # none down the lower sides
                continue
            up = (off @ SA) / SH
            if off @ SR > 0.05 and up < 0.75:                      # none on the inner edge, by the face
                continue
            out = off / np.linalg.norm(off)
            d = unit(*(out + SN * rnd.uniform(-0.25, 0.25) + SA * (0.25 if up > 0.7 else 0.7)))
            ln = (0.07 if up > 0.7 else 0.095) * (1 + 0.3 * rnd.uniform(-1, 1))
            r = 0.017 + 0.005 * rnd.uniform(0, 1)
            col = CRYS_RED if n % 2 == 0 else CRYS_VIOLET
            k.add(f"mxcrystal{n}", prism(p - out * 0.02, d, ln + 0.02, r), col, "armL", fuse=False, glow=col,
                  surface="shell")
            n += 1
    # 3. the emblem on the table
    c = SC + SN * 0.068
    k.add("mxemblem", lathe(c, SN, [(0, 0), (0.058, 0), (0.058, 0.006), (0, 0.006)], seg=40), STONE, "armL",
          fuse=False, glow=STONE)
    k.add("mxemblemring", ring_band(c + SN * 0.003, SN, 0.066, 0.012, thick=0.016), RING, "armL", fuse=False, glow=RING)
    k.add("mxemblemstar", slab([(x * 0.048, y * 0.048) for x, y in star()], c + SN * 0.009, SR, SA, 0.006), STAR,
          "armL", fuse=False)
    # 4. the clouds about the feet and the shield's foot, and a wisp at the +X side
    clouds(k, (-0.08, -0.04), 0.3, 0.18, 12, 0.054, [((0.115, -0.07), 0.1), ((-0.115, -0.07), 0.1)], 7, phase=0.15)
    wisp(k, 0.24, -10, 50, 0.22, 0.34, 0.028, 0.014, CRIMSON, "body", "a")


m = sibling("sableyemega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.sableye_mega(*args())
