# Camerupt-Mega-Mmax: Camerupt-Mega's own script (cameruptmega.py), whole and unchanged, with the Mega Dynamax form's
# additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cameruptmegammax.py -- <the export's Camerupt-Mega-Mmax.glb> <out.glb>
# From the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Camerupt.jpg), what it adds over the Mega:
#   1. violet crystals standing out of the volcano's slopes, each with a jet of pink and orange flame shooting up and
#      out beside it (here six clusters round the cone, so that the back camera sees them too);
#   2. fire and smoke rising from the crater;
#   3. the clouds of Max energy rolling about the feet, crimson and violet with cyan curls, crimson swirls about the
#      crater and a violet wisp along the flank;
#   4. the Key Stone's emblem glowing on the brow (a red gem in an orange ring), on the +X side the front camera sees.
# The cone's numbers are cameruptmega.py's (VY, VBX, VBY, VT, VH and body_top through the module).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST, LILAC = "#9660E0", "#C08CFA"
FLAME, YOLK = "#FF7448", "#FFE078"
SMOKE, SMOKE2 = "#8A8487", "#6C666B"
GEM, RING = "#E8305A", "#FFB040"


def join(shapes):
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in q] for q in f]); off += len(v)
    return np.vstack(V), F


def prism(base, d, length, r):
    """A six-sided crystal with a pointed tip, from base along d."""
    return lathe(np.asarray(base, float) - np.asarray(d) * r * 0.5, d,
                 [(0, 0), (r, r * 0.5), (r, length * 0.62), (0, length)], seg=6)


def ground_clouds(k, cy, rx, ry, s, n, seed, z0=0.0):
    """A ring of fused mounds on the ground: clumps of four puffs, crimson and violet by turns, cyan curls on some."""
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


def maxed(k, m):
    rs = np.random.RandomState(48)
    zb = m.body_top(0, m.VY) - 0.05
    top = zb + m.VH

    # 1. crystals and flame jets round the cone, half way up its slopes
    t = 0.5
    f = np.interp(t, [0, 0.3, 0.62, 1], [1, 0.62, 0.36, 0])
    rx, ry = m.VT + (m.VBX - m.VT) * f, m.VT + (m.VBY - m.VT) * f
    z = zb + m.VH * t
    crys, crys2, jets, cores = [], [], [], []
    for j, ang in enumerate((-140, -78, -18, 42, 100, 165)):
        a = math.radians(ang)
        base = np.array([rx * 0.92 * math.cos(a), m.VY + ry * 0.92 * math.sin(a), z])
        out_ = np.array([math.cos(a), math.sin(a) * rx / ry, 0.0]); out_ /= np.linalg.norm(out_)
        d = unit(out_[0] * 0.8, out_[1] * 0.8, 0.6)
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        crys.append(prism(base, d, 0.15, 0.036))
        crys2.append(prism(base + side * 0.022 - out_ * 0.01, unit(*(d + side * 0.45)), 0.1, 0.026))
        crys2.append(prism(base - side * 0.022 - out_ * 0.01, unit(*(d - side * 0.4 + np.array([0, 0, 0.2]))), 0.095,
                           0.025))
        # the jet: from behind the cluster, up and out, a little curved
        jd = unit(out_[0] * 0.55, out_[1] * 0.55, 0.84)
        j0 = base + jd * 0.02 + side * 0.01
        P = [j0 + jd * s_ + out_ * (0.02 * s_ / 0.12) ** 2 for s_ in np.linspace(0, 0.12, 6)]
        jets.append(outward(tube(P, [0.018, 0.02, 0.017, 0.013, 0.008, 0.003], seg=10, per=4)))
        Pc = [j0 + jd * s_ + out_ * 0.004 for s_ in np.linspace(0.02, 0.09, 4)]
        cores.append(outward(tube(Pc, [0.013, 0.014, 0.011, 0.004], seg=8, per=3)))
    k.add("mcrystal", join(crys), AMETHYST, "body", fuse=False, glow=AMETHYST, surface="shell")
    k.add("mcrystal2", join(crys2), LILAC, "body", fuse=False, glow=LILAC, surface="shell")
    k.add("mjet", join(jets), FLAME, "body", fuse=False, glow=FLAME)
    k.add("mjetcore", join(cores), YOLK, "body", fuse=False, glow=YOLK)

    # 2. fire out of the crater and smoke over it
    c = np.array([0, m.VY, top + 0.01])
    tongues = []
    for (dx, dy, h, r) in ((0, 0, 0.11, 0.045), (0.03, 0.015, 0.075, 0.03), (-0.028, -0.012, 0.08, 0.03)):
        P = [c + np.array([dx * (1 + u), dy * (1 + u), h * u]) for u in np.linspace(0, 1, 5)]
        tongues.append(outward(tube(P, [r, r * 0.95, r * 0.7, r * 0.4, 0.005], seg=10, per=4)))
    k.add("mfire", join(tongues), FLAME, "body", fuse=False, glow=FLAME)
    k.add("mfirecore", outward(tube([c + (0, 0, 0.01), c + (0, 0, 0.05), c + (0, 0, 0.075)], [0.026, 0.02, 0.004],
                                    seg=10, per=4)), YOLK, "body", fuse=False, glow=YOLK)
    # smoke: loose puffs (fused, the Mega's own paint of the bed between the boulders, everything over z 0.33 near the
    # back's middle, would turn them orange)
    for j, (dx, dy, dz, r) in enumerate(((0.028, 0.0, 0.095, 0.036), (-0.022, 0.012, 0.11, 0.032),
                                         (0.006, -0.008, 0.13, 0.028))):
        k.add(f"msmoke{j}", ell(c + (dx, dy, dz), (r, r, r * 0.85), sub=3), SMOKE if j % 2 == 0 else SMOKE2, "body",
              fuse=False)

    # 3. the clouds about the feet: the ring within 1.2 times the Mega's footprint (0.46 by 0.88), clear of the hooves
    # by 0.02 and kept behind y -0.355, where the Mega's own paint of the muzzle would turn them grey
    ground_clouds(k, 0.03, 0.238, 0.32, 0.042, 12, 323)
    # crimson swirls about the crater, a broken ring of small clumps
    for j, ang in enumerate((30, 150, 255)):
        a = math.radians(ang)
        p = np.array([0.13 * math.cos(a), m.VY + 0.15 * math.sin(a), top + 0.01])
        for q, (du, dz, r) in enumerate(((0, 0, 0.03), (0.035, 0.008, 0.024), (-0.03, -0.004, 0.02))):
            tang = np.array([-math.sin(a), math.cos(a), 0])
            k.add(f"mhalo{j}_{q}", ell(p + tang * du + (0, 0, dz), (r, r, r * 0.8), sub=3),
                  CRIMSON if q != 1 else DEEP, "body", fuse=False)
    # a violet wisp along the +X flank, standing off the skirt
    W = [(0.215, -0.16, 0.3), (0.235, -0.04, 0.33), (0.232, 0.08, 0.32), (0.215, 0.18, 0.36)]
    k.add("mwisp", outward(tube(W, [0.012, 0.022, 0.02, 0.006], seg=10, per=5)), VIOLET, "body", fuse=False)
    k.add("mwispedge", outward(tube([(0.222, -0.12, 0.312), (0.24, -0.03, 0.337), (0.236, 0.06, 0.33)],
                                    [0.008, 0.012, 0.005], seg=8, per=4)), CYAN, "body", fuse=False, glow=CYAN)

    # 4. the emblem on the brow, on the +X side over the eye, standing off the fur
    d = unit(0.5, -0.5, 0.71)
    e = m.H + m.HR * d * 1.02
    k.add("memblem", lathe(e, d, [(0, 0), (0.026, 0), (0.026, 0.01), (0, 0.016)], seg=6), GEM, "head",
          fuse=False, glow=GEM)
    k.add("memblemring", ring_band(e + d * 0.004, d, 0.032, 0.012, thick=0.008), RING, "head", fuse=False,
          glow=RING)


cm = sibling("cameruptmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the eyes' five states build after it
        _laid.append(True)
        maxed(self, cm)
    return _build(self, *a, **kw)
Kit.build = build

cm.cameruptmega(*args())
