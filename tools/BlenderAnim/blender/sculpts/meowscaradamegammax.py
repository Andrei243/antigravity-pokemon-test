# Mega Dynamax Meowscarada (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/meowscaradamegammax.py -- <the export's Meowscarada-Mega-Mmax.glb> <out.glb>
# Mega Meowscarada (meowscaradamega.py) whole and unchanged, with what the form's picture
# (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Meowscarada.jpg) lays over it, laid on the Mega's own kit just
# before it is meshed:
#   1. the flower bomb on the raised right paw's fingertip grown into a great rose of glowing pink crystal, about as
#      large as the head, with a few sparks of light round it (the bomb's own disc and petals stay under it, lit pink);
#   2. the clouds of Max energy: fused mounds of crimson and violet puffs about the feet, open at the front, a broken
#      ring of small crimson puffs over the head, a wisp by the left shoulder, cyan curls;
#   3. the Key Stone's emblem in the heart of the bloom at the throat: a glowing peach disc with a four-pointed star in a
#      pale pink ring.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, srgb, unit, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ROSE, ROSELIGHT, ROSEDEEP = "#E8509E", "#F276B8", "#C8287A"   # saturated: a glow renders pale pinks white
SPARK = "#FFF0FA"
STONE, RING = "#FFC490", "#FFC8E6"


def join(*shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def frame(d):
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)) if abs(d[2]) < 0.95 else np.cross(d, (1.0, 0, 0))
    a /= np.linalg.norm(a)
    return d, a, np.cross(d, a)


def petal(root, d, across, length, width, thick):
    """A petal of crystal: a flat-faced kite from its root out along d, widest at 0.55 of its length."""
    d, a = unit(*d), np.array(across, float)
    a = a - d * (a @ d); a /= np.linalg.norm(a)
    n = np.cross(d, a)
    root = np.array(root, float)
    pts = [root + a * width * 0.35, root - a * width * 0.35,
           root + d * length * 0.55 + a * width, root + d * length * 0.55 - a * width,
           root + d * length, root + d * length * 0.5 + n * thick, root + d * length * 0.5 - n * thick * 0.6]
    return hull(pts)


def rose(k, M):
    """The great rose of crystal over the flower bomb, on the raised paw's bone, its face tilted toward the front."""
    c = M.SPIN + np.array([0.012, -0.012, 0.085])
    up = unit(0.12, -0.45, 1)                       # the rose's axis: up and toward the front camera
    _, a, b = frame(up)
    inner, mid, outer = [], [], []
    # the bud: a tight cone of petals round the axis
    for i in range(5):
        t = i * 2 * math.pi / 5
        r = a * math.cos(t) + b * math.sin(t)
        inner.append(petal(c + r * 0.012 - up * 0.02, up * 1.0 + r * 0.25, np.cross(up, r), 0.085, 0.032, 0.014))
    # the middle ring opening out
    for i in range(6):
        t = i * 2 * math.pi / 6 + 0.4
        r = a * math.cos(t) + b * math.sin(t)
        mid.append(petal(c + r * 0.02 - up * 0.035, up * 0.85 + r * 0.55, np.cross(up, r), 0.1, 0.045, 0.016))
    # the outer ring, cupped round the rest
    for i in range(7):
        t = i * 2 * math.pi / 7 + 0.15
        r = a * math.cos(t) + b * math.sin(t)
        outer.append(petal(c + r * 0.03 - up * 0.05, up * 0.6 + r * 0.8, np.cross(up, r), 0.11, 0.052, 0.016))
    k.add("rosebud", join(*inner), ROSEDEEP, "armR", fuse=False, glow=ROSEDEEP, surface="plastic")
    k.add("rosemid", join(*mid), ROSE, "armR", fuse=False, glow=ROSE, surface="plastic")
    k.add("roseouter", join(*outer), ROSELIGHT, "armR", fuse=False, glow=ROSELIGHT, surface="plastic")
    # sparks of light round it: small four-sided crystals
    sparks = []
    for d, ln in (((1.0, -0.3, 0.6), 0.03), ((-0.7, -0.5, 0.9), 0.026), ((0.7, 0.3, 0.3), 0.028),
                  ((0.9, -0.6, -0.4), 0.024)):
        d = unit(*d)
        p = c + d * 0.13
        e, x, y = frame(d)
        sparks.append(hull([p - e * ln, p + e * ln, p + x * 0.011, p - x * 0.011, p + y * 0.011, p - y * 0.011]))
    k.add("rosesparks", join(*sparks), SPARK, "armR", fuse=False, glow=SPARK, surface="plastic")
    # the bomb's own disc and petals under the rose, lit as its calyx
    for p in k.parts:
        if p["name"].startswith(("bombdisc", "bombpetal")):
            p["color"] = srgb(ROSEDEEP)
            p["glow"] = ROSEDEEP


def emblem(k, M):
    """The Key Stone's emblem in the bloom's heart: a glowing disc with a star in a ring, standing just off it."""
    c = np.array([0.0, -0.078, 0.66]) + np.array([0.0, -0.034, 0.0])   # meowscaradamega.add_bloom's centre, before the heart
    d = unit(0.12, -1, 0.05)
    k.add("emblem", lathe(c, d, [(0, 0), (0.024, 0), (0.024, 0.008), (0, 0.008)]), STONE, "body", fuse=False,
          glow=STONE, surface="plastic")
    _, a, b = frame(d)
    star = []
    for t, ln in ((0, 0.024), (math.pi / 2, 0.024), (math.pi, 0.024), (3 * math.pi / 2, 0.024)):
        r = a * math.cos(t) + b * math.sin(t)
        q = np.cross(d, r)
        p0 = c + d * 0.006
        star.append(hull([p0 + q * 0.008, p0 - q * 0.008, p0 + r * ln, p0 + d * 0.008, p0 - d * 0.001]))
    k.add("emblemstar", join(*star), "#FFFFFF", "body", fuse=False, glow="#FFF6E0", surface="plastic")
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.036, 0.012, thick=0.009), RING, "body", fuse=False,
          glow=RING, surface="plastic")


def clouds(k):
    """Fused mounds of puffs about the feet (open at the front, over the cape's hem behind), on the root."""
    rnd = np.random.default_rng(11)
    n = 9
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i in range(n):
        a = math.radians(-125 + 250 * (i + 0.5) / n)          # 0 is straight behind (+Y); the front stays open
        cx, cy = 0.345 * math.sin(a), 0.06 + 0.3 * math.cos(a)
        col = cols[i % 4]
        for j in range(4):
            s = (0.075, 0.058, 0.05, 0.042)[j] * (1 + 0.15 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.22, -0.22, 0.05)[j]
            rx, ry = (0.345 + (0.0, 0.025, 0.015, -0.035)[j]), (0.3 + (0.0, 0.025, 0.015, -0.035)[j])
            p = np.array([rx * math.sin(t), 0.06 + ry * math.cos(t), s * (0.8, 0.9, 0.85, 1.9)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), col if j != 2 else cols[(i + 1) % 4], "root",
                  surface="cloth")
        if i % 3 == 1:
            q = np.array([cx * 1.08, cy * 1.08, 0.12])
            k.add(f"curl{i}", ell(q, (0.022, 0.022, 0.02), sub=2), CYAN, "root", fuse=False, glow=CYAN,
                  surface="cloth")
    # a broken ring of small crimson puffs over the head (none over the raised rose)
    for i, ang in enumerate((160, 225, 290)):
        t = math.radians(ang)
        c = np.array([0.17 * math.cos(t), -0.01 + 0.13 * math.sin(t), 1.04])
        e = np.array([math.cos(t + 1.57), math.sin(t + 1.57), 0.0])
        k.add(f"halo{i}", ell(c, (0.04, 0.034, 0.03), sub=3), CRIMSON if i % 2 == 0 else DEEP, "head",
              surface="cloth")
        k.add(f"halob{i}", ell(c + e * 0.038 + np.array([0, 0, -0.006]), (0.03, 0.026, 0.024), sub=3), CRIMSON,
              "head", surface="cloth")
        k.add(f"haloc{i}", ell(c - e * 0.034 + np.array([0, 0, 0.012]), (0.026, 0.024, 0.022), sub=3), DEEP,
              "head", surface="cloth")
    # a small cloud rising beside the left shoulder, standing off it
    for j, (p, r, col) in enumerate((((-0.2, 0.03, 0.7), 0.034, CRIMSON), ((-0.215, 0.0, 0.75), 0.028, VIOLET),
                                     ((-0.2, 0.05, 0.76), 0.024, DEEP))):
        k.add(f"wisp{j}", ell(p, (r, r, r * 0.9), sub=3), col, "body", fuse=False, surface="cloth")
    k.add("wispcurl", ell((-0.215, -0.02, 0.785), (0.015, 0.015, 0.014), sub=2), CYAN, "body", fuse=False,
          glow=CYAN, surface="cloth")


def maxed(k, M):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    rose(k, M)
    emblem(k, M)
    clouds(k)


M = sibling("meowscaradamega")
_build, _laid = Kit.build, []


def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, M)
    return _build(self, *a, **kw)


Kit.build = build

M.meowscarada_mega(*args())
