# Mega Dynamax Emboar (Emboar-Mega-Mmax), a form of this project's own, sculpted as Emboar-Mega's script with the
# Mega Dynamax form's additions laid on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/emboarmegammax.py -- <the export's Emboar-Mega-Mmax.glb> <out.glb>
# The Mega whole and unchanged, and over it, from the picture (Mega-Dynamax-Emboar.jpg; its raised arms and its
# missing staff are the picture's own redrawing of the body, not followed), largest first: a mane of pink crystal
# shards standing up through the mantle of flame round the neck and shoulders and hanging down over the chest; fire
# pouring back and up from each forearm; clouds of Max energy about the feet with cyan sparks, a broken ring of
# crimson puffs floating over the head; the Key Stone's emblem, a glowing disc in a pink ring, on the belly's panel.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, tube, lathe, ring_band, srgb, unit, sibling, args, hull

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PINK, HOT = "#F070B4", "#E8407E"
FLAME_RED, FLAME, FLAME_CORE = "#EC4A2C", "#F6782C", "#FFC83A"
STONE, STONERING = "#FFC08A", "#FF78B4"

# From emboar.py and emboarmega.py: the head H (0, -0.075, 0.672) r (0.133, 0.12, 0.103); the belly BEL (0, 0, 0.37)
# r (0.24, 0.205, 0.155), its yellow panel from z 0.255 to 0.49; the beard of flame hangs over the chest to z 0.385;
# the arms SH (+-0.205, 0, 0.555), EL (+-0.285, 0.015, 0.45), WR (+-0.295, -0.055, 0.335), the hands at
# (+-0.297, -0.085, 0.27); the staff stands in the right hand at x 0.3, y -0.1 from the ground to z 1.06; the voxel
# 0.0075.
m = sibling("emboarmega")
em = m.em
H = np.array([0.0, -0.075, 0.672])


def shard(base, d, length, w, flat=0.55):
    """A crystal shard: a flat-faced hull from a diamond root to a point, with a facet's kink part way."""
    base = np.array(base, float); d = unit(*d)
    a1 = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(a1) < 1e-3:
        a1 = np.array([1.0, 0, 0])
    a1 /= np.linalg.norm(a1); a2 = np.cross(d, a1)
    mid = base + d * length * 0.55
    return hull([base + a1 * w, base - a1 * w, base + a2 * w * flat, base - a2 * w * flat,
                 mid + a1 * w * 0.8 + a2 * w * 0.2, mid - a1 * w * 0.7, base - d * w * 0.4, base + d * length])


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # ---- the mane of pink crystal: standing up through the mantle at the sides and back, hanging over the chest ----
    i = 0
    for s in (1, -1):
        for (bx, by, bz, dx, dy, dz, ln, w) in (
                (0.135, -0.04, 0.6, 0.3, -0.1, 1.0, 0.36, 0.045),    # tall beside the head, before the mantle
                (0.18, -0.02, 0.59, 0.7, -0.15, 1.0, 0.28, 0.04),
                (0.1, 0.05, 0.62, 0.15, 0.3, 1.0, 0.3, 0.04),       # behind the head
                (0.21, 0.02, 0.58, 1.0, 0.1, 0.7, 0.22, 0.036),     # over the shoulder
                (0.17, -0.14, 0.57, 0.6, -0.6, -0.35, 0.2, 0.034),  # out over the chest's side
                (0.1, -0.19, 0.55, 0.35, -0.6, -0.8, 0.2, 0.032)):  # down over the chest
            k.add(f"mane{i}", shard((bx * s, by, bz), (dx * s, dy, dz), ln, w), PINK if i % 2 == 0 else HOT, "head",
                  fuse=False, glow=PINK if i % 2 == 0 else HOT, surface="shell")
            i += 1
    for j, (bx, ln) in enumerate(((-0.05, 0.19), (0.05, 0.2))):
        k.add(f"maneback{j}", shard((bx, 0.1, 0.62), (bx * 2, 0.55, 1.0), ln * 1.4, 0.04), HOT if j else PINK, "head",
              fuse=False, glow=HOT if j else PINK, surface="shell")

    # ---- fire pouring from each forearm, back and up ----------------------------------------------------------------
    EL, WR = np.array((0.285, 0.015, 0.45)), np.array((0.295, -0.055, 0.335))
    for s, side in ((1, "R"), (-1, "L")):
        mm = np.array([s, 1, 1.0])
        red, mid, core = em.Pieces(), em.Pieces(), em.Pieces()
        b = (WR * 0.55 + EL * 0.45 + (0.06, 0.0, 0.0)) * mm
        red.put(em.tongue(b, (0.35 * s, 0.5, 0.8), 0.26, 0.065, depth=0.05, bend=0.15))
        red.put(em.tongue(b + (0.0, 0.03, -0.06), (0.5 * s, 0.6, 0.35), 0.2, 0.055, depth=0.045, bend=0.3))
        mid.put(em.tongue(b + (0.01 * s, -0.01, 0.0), (0.4 * s, 0.4, 0.8), 0.2, 0.05, depth=0.04, bend=0.15))
        core.put(em.tongue(b + (0.015 * s, -0.025, 0.0), (0.4 * s, 0.35, 0.85), 0.14, 0.035, depth=0.03, bend=0.1))
        k.add(f"armfire{side}", red.shape(), FLAME_RED, f"arm{side}", fuse=False, glow=FLAME_RED)
        k.add(f"armfiremid{side}", mid.shape(), FLAME, f"arm{side}", fuse=False, glow=FLAME)
        k.add(f"armfirecore{side}", core.shape(), FLAME_CORE, f"arm{side}", fuse=False)

    # ---- the emblem on the belly's panel, under the beard ------------------------------------------------------------
    c = np.array([0.0, -0.212, 0.325])
    d = unit(0, -1, 0.05)
    k.add("emblem", lathe(c, d, [(0, -0.006), (0.04, -0.006), (0.04, 0.008), (0, 0.012)]), STONE, "body",
          fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.002, d, 0.05, 0.014, thick=0.011), STONERING, "body", fuse=False,
          glow=STONERING)

    # ---- the clouds: fused mounds in a ring about the feet, clear of them ---------------------------------------------
    rnd = np.random.default_rng(13)
    nm = 11
    CX, CY, RX, RY = 0.0, -0.02, 0.42, 0.34
    for i in range(nm):
        a = 2 * math.pi * (i + 0.5) / nm
        col = (CRIMSON, VIOLET, DEEP, DVIOLET)[i % 4]
        other = (VIOLET, CRIMSON, DVIOLET, CRIMSON)[i % 4]
        for j in range(4):
            sz = (0.075, 0.06, 0.05, 0.038)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.2, -0.2, 0.06)[j]
            dr = (0.0, 0.02, 0.015, -0.02)[j]
            p = (CX + (RX + dr) * math.cos(t), CY + (RY + dr) * math.sin(t), sz * (0.85, 0.9, 0.85, 2.6)[j])
            k.add(f"mcloud{i}_{j}", ell(p, (sz, sz, sz * 0.88)), other if j == 2 else col, "root")
        if i % 3 == 1:
            q = np.array([CX + RX * math.cos(a), CY + RY * math.sin(a), 0.1])
            o = unit(math.cos(a), math.sin(a), 0)
            tg = np.cross((0, 0, 1.0), o)
            pts = [q + o * 0.02 + (0, 0, 0.1), q + tg * 0.035 + (0, 0, 0.065), q - tg * 0.015 + o * 0.03 + (0, 0, 0.035),
                   q + tg * 0.04 + o * 0.05]
            for j in range(3):
                k.add(f"spark{i}_{j}", tube([pts[j], pts[j + 1]], [0.016, 0.016], seg=8), CYAN, "root", fuse=False,
                      glow=CYAN)

    # ---- a broken ring of crimson puffs floating over the head -------------------------------------------------------
    # (each a clump of flattened puffs overlapping heavily, so that it reads as a cloud and not as berries)
    for i, deg in enumerate((20, 75, 130, 200, 265)):
        a = math.radians(deg)
        cc = np.array([-0.02 + 0.2 * math.cos(a), 0.03 + 0.15 * math.sin(a), 0.98 + 0.02 * math.sin(2 * a)])
        tg = np.array([-math.sin(a), math.cos(a), 0.0])
        for j, (dx, dz, r) in enumerate(((0, 0, 0.04), (0.032, 0.006, 0.031), (-0.03, 0.004, 0.028),
                                         (0.008, 0.028, 0.024))):
            k.add(f"halo{i}_{j}", ell(cc + tg * dx + np.array([0, 0, dz]), (r * 1.15, r * 1.15, r * 0.8), sub=3),
                  CRIMSON if i % 2 == 0 else DEEP, "body", fuse=False)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.emboarmega(*args())
