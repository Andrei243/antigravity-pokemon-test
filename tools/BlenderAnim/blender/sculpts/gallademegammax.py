# Mega Dynamax Gallade (plan 48), a form of our own: Mega Gallade's own sculpt (gallademega.py, which builds as it is
# read) whole and unchanged, with what the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Gallade.jpg)
# lays over it, laid on the Mega's kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/gallademegammax.py -- <the export's Gallade-Mega-Mmax.glb> <out.glb>
# What the picture adds, largest first:
#   1. the cape turned to pale violet light (the Mega's own cape, recoloured and glowing: its shape is the Mega's);
#   2. the sword grown on past its tip into a long blade of cyan crystal, and a second, shorter crystal blade rising
#      up and back from the +X forearm's elbow (the picture's second blade behind the head);
#   3. the clouds of Max energy, a broken ring of crimson and violet mounds about the feet, and a wisp at each side;
#   4. the Key Stone's emblem glowing on the chest, beside the red horn, on the +X side the front camera sees.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, slab, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
CAPE_LIGHT, CAPE_GLOW = "#C8B4F4", "#B89CF0"
GLASS = "#8CF0F8"
STONE, RING, STAR = "#FFD890", "#FF6A50", "#FFF2C8"

# From gallademega.py (read, since it builds as it is read): the sword's root, its way and its length
SO = np.array([-0.19, -0.05, 0.62])
SD = unit(-0.62, -0.2, 0.76)
_sn = unit(-0.32, -1.0, 0.05); _sn = _sn - SD * (_sn @ SD); SN = _sn / np.linalg.norm(_sn)
SA = np.cross(SN, SD)
if SA[2] < 0:
    SA = -SA
SL = 0.5
FEET = [((0.346, -0.075), 0.11), ((-0.346, -0.075), 0.11), ((0.29, -0.01), 0.06), ((-0.29, -0.01), 0.06)]


def crystal(base, d, length, half, thick, side):
    """A long blade of crystal: a lozenge in plan (narrow root, broad shoulder, a point), flat faced."""
    d = unit(*d); side = np.array(side, float); side = side - d * (side @ d); side /= np.linalg.norm(side)
    n = np.cross(d, side)
    base = np.array(base, float)
    pts = []
    for t, w in ((0.0, 0.45), (0.25, 1.0), (0.8, 0.85)):
        c = base + d * length * t
        for sgn in (1, -1):
            pts.append(c + side * half * w * sgn)
        pts.append(c + n * thick / 2); pts.append(c - n * thick / 2)
    pts.append(base + d * length)
    return hull(pts)


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


def emblem(k, c, d, r, bone, star=False):
    """The Key Stone's emblem: a glowing disc in a glowing ring of another colour, loose, a hair off the body."""
    d = unit(*d)
    k.add("mxemblem", lathe(c, d, [(0, 0), (r * 0.78, 0), (r * 0.78, 0.008), (0, 0.008)], seg=40), STONE, bone,
          fuse=False, glow=STONE)
    k.add("mxemblemring", ring_band(c + d * 0.004, d, r * 0.9, 0.012, thick=r * 0.28), RING, bone, fuse=False,
          glow=RING)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # 1. the cape of pale violet light: the Mega's own cape, its colour and its light only
    for p in k.parts:
        if p["name"] == "cape":
            p["color"] = srgb(CAPE_LIGHT)
            p["glow"] = CAPE_GLOW
    # 2. the crystal blades: the sword grown on past its tip, its blade a little more outward than the red one's
    d1 = unit(*(SD + np.array([0.12, 0.0, 0.2])))
    b1 = SO + SD * SL * 0.42 + SA * 0.06 + SN * 0.0
    k.add("mxblade", crystal(b1, d1, 0.45, 0.05, 0.042, SA), GLASS, "armL", fuse=False, glow=GLASS)
    # the second, from the +X forearm's elbow, up and back over the shoulder
    k.add("mxblade2", crystal((0.19, -0.01, 0.635), (0.35, 0.55, 0.78), 0.36, 0.036, 0.038, (1, 0, 0.2)), GLASS,
          "armR", fuse=False, glow=GLASS)
    # 3. the clouds about the feet, clear of the feet, and a wisp at each side
    clouds(k, (0.0, 0.0), 0.54, 0.34, 10, 0.075, FEET, 3, phase=0.1)
    wisp(k, 0.34, 150, 215, 0.42, 0.56, 0.04, 0.02, CRIMSON, "body", "a")
    wisp(k, 0.36, -20, -70, 0.3, 0.4, 0.04, 0.02, VIOLET, "body", "b")
    # 4. the emblem on the chest, beside the horn on the +X side
    emblem(k, (0.042, -0.05, 0.703), (0.45, -1, 0.12), 0.036, "body")


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("gallademega")          # builds as it is read, with this script's own arguments
