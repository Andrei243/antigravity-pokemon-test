# Mega Dynamax Incineroar (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of
# the form (sculptkit.py): Incineroar-Mega's own script, whole and unchanged (sibling("incineroarmega")), with the
# form's additions laid on its kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/incineroarmegammax.py -- <the export's Incineroar-Mega-Mmax.glb> <out.glb>
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Incineroar.jpg: a spinning band of fire and red
# crystal shards ringing the hips under the champion's belt (its largest addition), the belt's gold plate lit, a great
# flame on the end of its tail and small ones at its ears, the Key Stone's emblem on the belt's side plate the front
# camera sees, clouds of Max energy rolling about the feet, and smoke curling up behind the shoulders and over the
# head. The sculptor's list (PokemonModels.MegaMax2.cs, IncineroarMmax) is the same.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, srgb, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
FIRE, HEART, SHARD = "#F6702A", "#FFDE6E", "#E8402E"      # the band of fire, its bright heart and its red shards
PLATE = "#FFD45A"                                         # the belt's gold plate, lit
STONE, RING = "#FFDE6E", "#F6EEFF"


def keep_clear(c, s, keep):
    P, R = keep
    return float(np.min(np.linalg.norm(P - c, axis=1) - R)) - s


def start(c, rx, ry, a, H, keep, gap):
    """How far out (a share of the ring) a clump at angle a must stand to clear keep."""
    s, f = H * 0.072, 1.0
    for _ in range(40):
        p = np.array([c[0] + rx * f * math.cos(a), c[1] + ry * f * math.sin(a), s * 0.75])
        if keep_clear(p, s, keep) >= gap:
            break
        f += 0.02
    return f


def clouds(k, c, rx, ry, H, n, keep, gap, seed, phase=0.0):
    """A ring of fused mounds on the ground: clumps of four round puffs, each pushed out until it stands clear of keep."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + phase
        f0 = start(c, rx, ry, a, H, keep, gap)
        for j in range(4):
            s = H * (0.072, 0.056, 0.046, 0.036)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.15, -0.14, 0.04)[j] * (0.33 / max(rx, ry))
            f = f0 + (0.0, 0.04, 0.03, -0.045)[j]
            z = s * (0.75, 0.9, 0.8, 1.9)[j]
            for _ in range(40):
                p = np.array([c[0] + rx * f * math.cos(t), c[1] + ry * f * math.sin(t), z])
                if keep_clear(p, s, keep) >= gap:
                    break
                f += 0.02
            col = cols[i % 3] if j != 2 else cols[(i + 1) % 3]
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), col, "root")
            if j == 0 and i % 3 == 0:
                q = p + np.array([0.0, 0.0, s * 1.0]) + np.array([math.cos(t), math.sin(t), 0]) * s * 0.5
                k.add(f"curl{i}", outward(ell(q, (s * 0.42, s * 0.42, s * 0.36), sub=3)), CYAN, "root", fuse=False,
                      glow=CYAN)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a glowing disc in a glowing ring, a hair off the body."""
    k.add("emblem", lathe(c, d, [(0, 0), (r, 0), (r, 0.005), (0, 0.005)]), STONE, bone, fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.0025, d, r * 1.25, 0.007, thick=r * 0.32), RING, bone, fuse=False,
          glow=RING)


def ring_pt(a, RX, RY, C, tilt):
    """A point of the band of fire, its front (a = -90 degrees) lower than its back by tilt."""
    return np.array([C[0] + RX * math.cos(a), C[1] + RY * math.sin(a), C[2] + tilt * math.sin(a)])


def maxed(k, m):
    ib = m.ib
    H = 0.81
    # the belt's plate lit, its paw print left dark gold
    for p in k.parts:
        if p["name"] == "beltplate":
            p["color"] = srgb(PLATE); p["glow"] = PLATE
    # (a) the band of fire round the hips, under the belt and the hands, its front a little lower; red shards on it
    C, RX, RY, tilt = np.array([0.0, 0.0, 0.265]), 0.26, 0.23, 0.035
    a0 = math.pi / 2                                   # the band's seam behind
    ring = [ring_pt(a0 + 2 * math.pi * i / 48, RX, RY, C, tilt) for i in range(49)]
    ring.append(ring_pt(a0 + 2 * math.pi * 49 / 48, RX, RY, C, tilt))
    k.add("fireband", outward(tube(ring, [0.016] * len(ring), seg=12, per=2)), FIRE, "body", fuse=False, glow=FIRE)
    heart = [ring_pt(a0 + 2 * math.pi * i / 48, RX * 1.06, RY * 1.06, C + (0, 0, 0.022), tilt) for i in range(50)]
    k.add("fireheart", outward(tube(heart, [0.011] * len(heart), seg=10, per=2)), HEART, "body", fuse=False,
          glow=HEART)
    for i in range(16):
        a = a0 + 2 * math.pi * (i + 0.3) / 16
        p = ring_pt(a, RX, RY, C, tilt)
        tang = unit(-RX * math.sin(a), RY * math.cos(a), tilt * math.cos(a))
        outv = unit(math.cos(a) / RX, math.sin(a) / RY, 0)
        L = 0.1 + 0.03 * (i % 3)
        tip = p + tang * L + outv * 0.025 + np.array([0, 0, 0.02 * ((i % 2) * 2 - 1)])
        k.add(f"shard{i}", hull([p - tang * 0.01 + outv * 0.024, p - tang * 0.01 - outv * 0.016,
                                 p + np.array([0, 0, 0.026]), p - np.array([0, 0, 0.02]), tip]),
              SHARD if i % 2 else FIRE, "body", fuse=False, glow=SHARD if i % 2 else FIRE)
    # the great flame on the end of the tail, and small ones at the ears
    pts = ib.lt.tail_pts(ib.TAIL)
    end = np.array(pts[-1])
    red, mid, core = ib.Pieces(), ib.Pieces(), ib.Pieces()
    red.put(ib.tongue(end - (0, 0.01, 0.01), (0.1, 0.2, 1.0), 0.24, 0.06, bend=0.05))
    mid.put(ib.tongue(end + (0.02, 0.0, 0.0), (0.45, 0.25, 0.9), 0.16, 0.045, bend=0.05))
    core.put(ib.tongue(end + (0, -0.012, 0.01), (0.1, 0.15, 1.0), 0.15, 0.034))
    k.add("maxflamered", red.shape(), ib.FLAME_RED, "tail", fuse=False, glow=ib.FLAME_RED)
    k.add("maxflamemid", mid.shape(), ib.FLAME, "tail", fuse=False, glow=ib.FLAME)
    k.add("maxflamecore", core.shape(), ib.FLAME_CORE, "tail", fuse=False, glow=ib.FLAME_CORE)
    ered, ecore = ib.Pieces(), ib.Pieces()
    for s in (1, -1):
        mm = np.array([s, 1, 1.0])
        root = ib.H + np.array([0.075, 0.02, 0.07]) * mm
        tip = root + unit(0.35 * s, 0.85, 0.55) * 0.08
        ered.put(ib.tongue(tip, (0.2 * s, 0.3, 1.0), 0.055, 0.022))
        ecore.put(ib.tongue(tip + (0, -0.004, 0.004), (0.2 * s, 0.3, 1.0), 0.035, 0.014))
    k.add("earflame", ered.shape(), ib.FLAME_RED, "head", fuse=False, glow=ib.FLAME_RED)
    k.add("earflamecore", ecore.shape(), ib.FLAME_CORE, "head", fuse=False, glow=ib.FLAME_CORE)
    # (c) the emblem on the belt's +X side plate (incineroarmega.py's beltstud), on the side the front camera sees
    th = math.radians(-90 + 40)
    c = np.array([m.BA * math.cos(th), m.WAIST[1] + m.BB * math.sin(th), m.BELT])
    outv = unit(math.cos(th) / m.BA, math.sin(th) / m.BB, 0)
    emblem(k, c + outv * 0.0175, outv, 0.017, "body")
    # (b) the clouds about the feet; smoke curling up behind the shoulders and a broken ring over the head
    pts, rads = [], []
    for sx in (1, -1):
        for P, R in (((0.075, -0.02, 0.04), 0.075), ((0.08, -0.07, 0.03), 0.06), ((0.07, 0.03, 0.05), 0.07),
                     ((0.07, 0.0, 0.15), 0.08)):
            pts.append(np.array(P) * np.array([sx, 1, 1])); rads.append(R)
    clouds(k, (0.0, 0.0), 0.25, 0.24, H, 9, (np.array(pts), np.array(rads)), 0.02, 9, phase=-0.17)
    for s, side in ((1, "R"), (-1, "L")):
        for j in range(4):
            q = np.array([(0.2 + 0.02 * j) * s, 0.09 + 0.012 * j, 0.6 + 0.045 * j])
            k.add(f"wisp{side}{j}", outward(ell(q, (0.034 - 0.004 * j,) * 3, sub=3)),
                  (CRIMSON, DEEP, VIOLET, CRIMSON)[(j + (s > 0)) % 4], "body", fuse=False)
    top = ib.H + np.array([0.0, 0.03, 0.15])
    for i in range(7):
        a = 2 * math.pi * i / 7
        if abs(math.sin(a) + 1) < 0.25:                 # the ring broken at the front
            continue
        s = 0.022 if i % 2 == 0 else 0.017
        q = top + np.array([0.09 * math.cos(a), 0.09 * math.sin(a), 0.006 * math.sin(3 * a)])
        k.add(f"crown{i}", outward(ell(q, (s, s, s * 0.85), sub=3)), CRIMSON if i % 3 else DEEP, "head", fuse=False)


m = sibling("incineroarmega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.incineroarmega(*args())
