# Mega Dynamax Medicham (plan 48), a form of our own: Mega Medicham's own sculpt (medichammega.py, which builds as it
# is read) whole and unchanged, with what the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Medicham.jpg)
# lays over it, laid on the Mega's kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/medichammegammax.py -- <the export's Medicham-Mega-Mmax.glb> <out.glb>
# What the picture adds, largest first:
#   1. the four ribbons of its aura turned to wings of pale prismatic light: the Mega's own ribbons recoloured and
#      glowing (their shape is the Mega's), each with two streaks of light along it, gold and rose, thicker than the
#      ribbon so that they show on both faces (the picture's iridescence);
#   2. the clouds of Max energy, a ring of crimson and violet mounds under the floating Pokémon, and a wisp rising at
#      each side (the picture's swirl);
#   3. the Key Stone's emblem on the brow, over the headdress's gold drop, which it stands before.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, sheet, lathe, ring_band, srgb, unit, smoothstep, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
WING, WING_GLOW = "#E2D0FA", "#D8C2FF"
GOLDS, ROSE = "#FFCC66", "#FF94D2"
PETAL_GLASS, PETAL_GLOW = "#EEE2FC", "#E0CCFF"
STONE, RING = "#FFF0C8", "#D890FF"


def ribbon(s, z0, rise, length, back, w0, w1, phase, vlo=0.0, vhi=1.0, ulo=0.0, uhi=1.0, thick=0.016):
    """medichammega.py's ribbon (copied: that script builds as it is read), with a band of it in v and u."""
    def at(u, v):
        u = ulo + (uhi - ulo) * u
        v = vlo + (vhi - vlo) * v
        uu = 0.02 + 0.98 * u
        step = 1 - 0.07 * smoothstep(0.4, 0.6, 1 - v) * smoothstep(0.85, 1.0, uu)
        q = uu * step
        x = s * (0.03 + length * q)
        y = 0.03 + back * q ** 1.2
        z = z0 + rise * q ** 1.3 + 0.022 * np.sin(2 * math.pi * (1.0 * q + phase)) * smoothstep(0.1, 0.4, q)
        w = w0 + (w1 - w0) * q
        z = z + (v - 0.5) * w
        y = y - (v - 0.5) * w * 0.25 * np.sin(2 * math.pi * (1.4 * q + phase + 0.25))
        return np.stack([x, y, z], axis=-1)
    return sheet(at, 60, 4, thick)


RIBBONS = {"R0": (1, 0.645, 0.3, 0.4, 0.14, 0.05, 0.1, 0.0), "L0": (-1, 0.645, 0.3, 0.4, 0.14, 0.05, 0.1, 0.0),
           "R1": (1, 0.585, 0.12, 0.4, 0.16, 0.046, 0.092, 0.4), "L1": (-1, 0.585, 0.12, 0.4, 0.16, 0.046, 0.092, 0.4)}


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
            k.paint(lambda P: np.ones(len(P)), pc, on=[name])      # the Mega's own paints are kept off the clouds
        if i % 3 == 0:
            q = np.array([centre[0] + (rx + 0.03) * math.cos(a), centre[1] + (ry + 0.03) * math.sin(a), size * 1.25])
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


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a glowing disc in a glowing ring of another colour, loose, a hair off the body."""
    d = unit(*d)
    k.add("mxemblem", lathe(c, d, [(0, 0), (r * 0.78, 0), (r * 0.78, 0.009), (0, 0.009)], seg=40), STONE, bone,
          fuse=False, glow=STONE)
    k.add("mxemblemring", ring_band(c + d * 0.0045, d, r * 0.9, 0.013, thick=r * 0.28), RING, bone, fuse=False,
          glow=RING)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # 1. the wings of prismatic light: the Mega's ribbons, their colour and their light, and two streaks along each
    for p in k.parts:
        if p["name"].startswith("ribbon"):
            p["color"] = srgb(WING)
            p["glow"] = WING_GLOW
        # the headdress's petals, the picture's two small upright wings at the head among them, turned to glass
        if p["name"].startswith("petal"):
            p["color"] = srgb(PETAL_GLASS)
            p["glow"] = PETAL_GLOW
    for tag, a in RIBBONS.items():
        # two broad bands of light along each wing, gold above and rose below, a pale line of the wing between
        k.add(f"mxstreakg{tag}", ribbon(*a, vlo=0.54, vhi=0.97, ulo=0.06, uhi=0.96, thick=0.026), GOLDS,
              f"ribbon{tag}", fuse=False, glow=GOLDS)
        k.add(f"mxstreakr{tag}", ribbon(*a, vlo=0.03, vhi=0.4, ulo=0.16, uhi=0.99, thick=0.026), ROSE,
              f"ribbon{tag}", fuse=False, glow=ROSE)
    # 2. the clouds under it, and a wisp rising at each side; none in front of the low ball, where the Mega's gold
    # spot spotL1 is cast from below
    clouds(k, (0.0, 0.0), 0.32, 0.24, 9, 0.058, [((0.05, -0.028), 0.03), ((0.02, -0.3), 0.07)], 5, phase=0.2)
    wisp(k, 0.27, 15, 75, 0.22, 0.38, 0.034, 0.016, CRIMSON, "body", "a")
    wisp(k, 0.28, 190, 120, 0.26, 0.38, 0.034, 0.016, VIOLET, "body", "b")
    # 3. the emblem on the brow, standing before the gold drop
    emblem(k, (0.0, -0.1, 0.835), (0, -1, 0.1), 0.042, "head")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("medichammega")         # builds as it is read, with this script's own arguments
