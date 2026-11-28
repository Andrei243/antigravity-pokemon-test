# Glalie-Mega-Mmax: Glalie-Mega's own script (glaliemega.py, which builds as it is read), whole and unchanged in shape,
# with the Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glaliemegammax.py -- <the export's Glalie-Mega-Mmax.glb> <out.glb>
# From the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Glalie.jpg), what it adds over the Mega:
#   1. fangs of glowing ice crystal about the great maw: two long ones hanging from beside the upper teeth, four rising
#      from the lower teeth (the largest addition);
#   2. the crystal horns on its brow lit from within, and crystal spikes of ice standing down and out of the egg's
#      lower rim;
#   3. the Key Stone's emblem glowing on the brow, a cyan ring round a pale star, on the +X side of the middle;
#   4. the clouds of Max energy under the hovering egg and crimson wisps curling up its sides.
# The Mega's numbers, read out of glaliemega.py: the egg C (0, 0, 0.385) with half sizes R (0.262, 0.24, 0.29), the maw
# MC (0, 0.3) and MR (0.185, 0.19) in (x, z), the upper teeth at z 0.425 for x in -0.1..0.1, the lower teeth on the
# maw's lower half at 0.86 of its size.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, slab, hull, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
ICEGLOW, ICEPALE = "#82E8FA", "#C8F6FF"
STAR, RING, DISC = "#FFFFFF", "#5CE0F6", "#2A6EA8"

C = np.array([0.0, 0.0, 0.385])
R = np.array([0.262, 0.24, 0.29])
MC = np.array([0.0, 0.3])
MR = np.array([0.185, 0.19])


def front_y(x, z):
    q = 1 - (x / R[0]) ** 2 - ((z - C[2]) / R[2]) ** 2
    return -R[1] * math.sqrt(max(q, 0.0))


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def fang(root, tip, w, t, face=(0, -1, 0)):
    """A crystal fang: a flat-faced blade, a kite in section, widest near its root."""
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; L = np.linalg.norm(d); d /= L
    n = np.array(face, float); n = n - d * (n @ d); n /= np.linalg.norm(n)
    s = np.cross(n, d)
    pts = [root + s * w * 0.8, root - s * w * 0.8, root + n * t * 0.5, root - n * t * 0.5]
    m = root + d * L * 0.25
    pts += [m + s * w, m - s * w, m + n * t * 0.8, m - n * t * 0.8, tip]
    return hull(np.array(pts))


def ground_clouds(k, cy, rx, ry, s, n, seed, z0=0.0):
    rnd = np.random.default_rng(seed)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        col = (CRIMSON, VIOLET, DEEP, CRIMSON, DEEPV, VIOLET)[i % 6]
        other = (VIOLET, DEEP, CRIMSON, DEEPV, CRIMSON, DEEP)[i % 6]
        r0 = math.hypot(rx * math.cos(a), ry * math.sin(a))
        for j in range(4):
            sz = s * (1.0, 0.76, 0.64, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 1.3, -1.2, 0.25)[j] * s / r0
            f = 1 + (0.0, 0.25, 0.15, -0.35)[j] * s / r0
            p = np.array([rx * f * math.cos(t), cy + ry * f * math.sin(t), z0 + sz * (0.72, 0.82, 0.78, 1.95)[j]])
            k.add(f"mcloud{i}_{j}", ell(p, (sz, sz, sz * 0.88)), other if j == 2 else col, "root")
        if i % 2 == 0:
            q = np.array([rx * 1.04 * math.cos(a), cy + ry * 1.04 * math.sin(a), z0 + s * 1.55])
            curls.append(ell(q, (s * 0.34, s * 0.34, s * 0.28), sub=3))
    k.add("mcurls", join(curls), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    # 1. the crystal fangs about the maw
    big, small = [], []
    for s in (1, -1):
        x, z = 0.128 * s, 0.44
        y = front_y(0.128, z) - 0.03
        big.append(fang((x, y, z), (0.112 * s, y - 0.035, z - 0.19), 0.036, 0.034))
    for i, a in enumerate((-1.3, -0.55, 0.55, 1.3)):
        x, z = MC[0] + 0.84 * MR[0] * math.sin(a), MC[1] - 0.84 * MR[1] * math.cos(a)
        y = front_y(x, z) - 0.03
        to = np.array([MC[0] - x, 0, MC[1] - z]); to /= np.linalg.norm(to)
        ln = 0.13 if abs(a) < 1 else 0.11
        tip = np.array([x, y - 0.03, z]) + to * ln + np.array([0, 0, 0.02])
        small.append(fang((x, y, z), tip, 0.03, 0.03))
    k.add("mfang", join(big), ICEGLOW, "body", fuse=False, glow=ICEGLOW, surface="shell")
    k.add("mfanglow", join(small), ICEPALE, "body", fuse=False, glow=ICEPALE, surface="shell")

    # 2. the brow's crystal horns lit from within; ice spikes on the lower rim
    for p in k.parts:
        if p["name"].startswith("crystal"):
            p["glow"] = "#5CC8EE" if p["name"].startswith("crystalR") or p["name"].startswith("crystalL") \
                else "#A6E4F8"
    spikes = []
    for i, a in enumerate((-2.2, -1.1, 0.0, 1.1, 2.2)):
        # a: round from straight down at the egg's front-lower rim
        dx, dz = math.sin(a), -math.cos(a)
        root = np.array([dx * 0.2, -0.13 + 0.03 * abs(dx), C[2] + dz * 0.23])
        out_ = np.array([dx * 0.8, -0.45, dz * 0.75]); out_ /= np.linalg.norm(out_)
        spikes.append(fang(root, root + out_ * 0.13, 0.03, 0.03, face=(0, -1, 0.2)))
    k.add("mspike", join(spikes), ICEGLOW, "body", fuse=False, glow=ICEGLOW, surface="shell")

    # 3. the emblem on the brow, between the horns and over the +X brow
    d = unit(0.25, -0.72, 0.65)
    e = np.array([0.035, front_y(0.035, 0.638) - 0.012, 0.638])
    k.add("memblem", lathe(e, d, [(0, 0), (0.026, 0), (0.026, 0.007), (0, 0.011)], seg=24), DISC, "body",
          fuse=False, glow=DISC)
    k.add("memblemring", ring_band(e + d * 0.003, d, 0.032, 0.013, thick=0.009), RING, "body", fuse=False,
          glow=RING)
    ex = np.cross(d, (0, 0, 1.0)); ex /= np.linalg.norm(ex)
    ez = np.cross(ex, d)
    star = [((0.022 if i % 2 == 0 else 0.007) * math.cos(math.pi / 2 + i * math.pi / 4),
             (0.022 if i % 2 == 0 else 0.007) * math.sin(math.pi / 2 + i * math.pi / 4)) for i in range(8)]
    k.add("memblemstar", slab(star, e + d * 0.01, ex, ez, 0.006), STAR, "body", fuse=False, glow=STAR)

    # 4. the clouds under the egg (it hovers, its lowest point at 0.095) and wisps up its sides
    ground_clouds(k, 0.0, 0.25, 0.24, 0.058, 11, 362)
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        # a wisp curling up each side: a column of shrinking loose puffs rising from the clouds, clear of the egg
        for j, (x, y, z, r) in enumerate(((0.3, 0.01, 0.15, 0.045), (0.315, 0.03, 0.22, 0.037), (0.31, 0.05, 0.28, 0.03),
                                          (0.29, 0.065, 0.33, 0.024))):
            k.add(f"mwisp{sd}{j}", ell((x * s, y, z), (r, r, r * 0.9), sub=3),
                  (CRIMSON, DEEP, CRIMSON, VIOLET)[j] if s > 0 else (DEEP, VIOLET, CRIMSON, DEEP)[j], "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

sibling("glaliemega")
