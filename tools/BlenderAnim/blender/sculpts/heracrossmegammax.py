# Heracross-Mega-Mmax: Mega Heracross (heracrossmega.py, built unchanged) with what its Mega Dynamax form lays over it,
# added to the Mega's own kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/heracrossmegammax.py -- <the export's Heracross-Mega-Mmax.glb> <out.glb>
#
# From the picture (Mega-Dynamax-Heracross.jpg): (1) a great crown of violet and lilac crystals fanned up and out from
# behind the head, the largest addition, standing on (2) a glowing orange halo round the top of the head; (3) the Key
# Stone's emblem on the upper belly, a pale disc with a violet star in a glowing pink ring; (4) clouds of Max energy,
# crimson and violet with cyan curls, rolling about the feet and a wisp beside each shoulder.
# Numbers of the Mega taken from heracrossmega.py (its parts are local to its function): head (0, -0.075, 0.6) r 0.1,
# horn foot (0, -0.13, 0.66), fists (+-0.305, -0.06, 0.72) r 0.158, body (0, 0, 0.34) r (0.23, 0.2, 0.225),
# feet (+-0.175, -0.05, 0.04) r (0.065, 0.085, 0.042); the yellow paint low on the back (|x| < 0.11, y > 0.07, z < 0.22)
# is kept clear of the clouds.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, axes, sibling, args

CRIMSON, DEEPCRIMSON, VIOLET, DEEPVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AMETHYST, LILAC, HALO = "#9660E0", "#C496FA", "#FF9A5A"
RING, DISC, STAR = "#FF8CC8", "#F6E8FF", "#8E4CE0"


def crystal(base, tip, r, seg=6, shoulder=0.78):
    """A six-sided crystal from base to tip: a prism r across its flats with a pointed end (a flat-faced hull)."""
    base, tip = np.array(base, float), np.array(tip, float)
    a, b = axes(tip - base)
    mid = base + (tip - base) * shoulder
    pts = [tip]
    for j in range(seg):
        t = 2 * math.pi * (j + 0.5) / seg
        o = r * (math.cos(t) * a + math.sin(t) * b)
        pts += [base + o, mid + o]
    return hull(pts)


def star4(c, d, r, thick):
    """A four-pointed star standing on d at c (a flat-faced hull of two crossed kites)."""
    a, b = axes(d)
    c, d = np.array(c, float), np.array(d, float)
    pts = []
    for e in (a, -a, b, -b):
        pts += [c + e * r, c + e * r + d * thick]
    for e in (a + b, a - b, -a + b, -a - b):
        pts += [c + e * r * 0.2, c + e * r * 0.2 + d * thick * 1.6]
    return hull(pts)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a pale disc with a star, in a glowing ring, a hair off the body."""
    d = np.array(d, float) / np.linalg.norm(d)
    k.add("emblemdisc", lathe(c, d, [(0, 0), (r * 0.78, 0), (r * 0.78, 0.008), (0, 0.011)]), DISC, bone,
          fuse=False, glow=DISC)
    k.add("emblemring", lathe(c, d, [(r * 0.72, 0), (r, 0), (r, 0.012), (r * 0.72, 0.012)], seg=48), RING, bone,
          fuse=False, glow=RING)
    k.add("emblemstar", star4(c + d * 0.009, d, r * 0.62, 0.006), STAR, bone, fuse=False, glow=STAR)


def gap(p, s, avoid):
    """Roughly how far a puff of radius s at p stands clear of the ellipsoids in avoid."""
    g = 9.0
    for c, R in avoid:
        q = (p - np.array(c)) / np.array(R)
        g = min(g, (np.linalg.norm(q) - 1) * min(R) - s)
    return g


def clouds(k, cx, cy, rx, ry, n, size, avoid, seed, skip=lambda x, y: False, bone="root", clear=0.03, curls=(0, 3)):
    """A broken ring of fused cloud mounds on the ground: clumps of round puffs, crimson and violet by turns, each
    clear of the body by `clear` (three cells) so the remesh keeps them islands; a few cyan curls ride on them."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEPCRIMSON, CRIMSON, DEEPVIOLET)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n + 0.15 * rnd.uniform(-1, 1)
        col = cols[i % len(cols)]
        other = (VIOLET, CRIMSON, DEEPVIOLET, VIOLET, CRIMSON)[i % 5]
        laid = []
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.34, -0.3, 0.06)[j] * (0.5 / max(rx, ry))
            rr = (0.0, 0.02, 0.015, -0.01)[j]
            x, y = cx + (rx + rr) * math.cos(t), cy + (ry + rr) * math.sin(t)
            if skip(x, y):
                continue
            z = (0.78, 0.7, 0.7, 1.75)[j] * s
            p = np.array([x, y, z])
            # pushed out from the ring's middle until it stands clear of the body
            out_d = np.array([math.cos(t) / max(rx, 1e-3), math.sin(t) / max(ry, 1e-3), 0])
            out_d /= np.linalg.norm(out_d)
            for _ in range(30):
                if gap(p, s, avoid) >= clear:
                    break
                p = p + out_d * 0.006
            if gap(p, s, avoid) < clear:
                continue
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88)), other if j == 2 else col, bone)
            laid.append((p, s))
        if laid and i % curls[1] == curls[0]:
            p, s = laid[0]
            q = p + np.array([0, 0, s * 0.95])
            k.add(f"curl{i}", ell(q, (size * 0.3, size * 0.3, size * 0.26), sub=3), CYAN, bone, fuse=False, glow=CYAN)


def wisp(k, name, c, size, bone, n=3, toward=(0, 0, 1)):
    """A small wisp of crimson puffs in the air: a short trail of fused balls standing alone."""
    t = np.array(toward, float); t /= np.linalg.norm(t)
    for j in range(n):
        s = size * (1.0, 0.88, 0.76, 0.62)[j]
        k.add(f"{name}{j}", ell(np.array(c) + t * size * 1.05 * j, (s, s, s * 0.9)),
              (CRIMSON, DEEPCRIMSON, VIOLET, CRIMSON)[j], bone)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Heracross: called with the Mega's kit just before it is meshed."""
    # (1) the crown of crystals: a fan rising from behind the head, up and out, the outer ones kept over the fists
    C = np.array([0.0, 0.03, 0.66])
    fan = [(-66, 0.3, 0.048), (-53, 0.36, 0.054), (-39, 0.42, 0.058), (-25, 0.44, 0.06), (-10, 0.5, 0.062),
           (6, 0.46, 0.062), (20, 0.45, 0.06), (34, 0.42, 0.058), (48, 0.37, 0.054), (62, 0.31, 0.048)]
    for i, (deg, L, r) in enumerate(fan):
        th = math.radians(deg)
        # steep at the root (clear of the fists), the outer ones leaning out over them
        d = unit(math.sin(th) * 0.95, 0.14, math.cos(th) * 1.15)
        base = C + np.array([math.sin(th) * 0.09, 0.02 + 0.025 * (i % 2), math.cos(th) * 0.07])
        k.add(f"crown{i}", crystal(base - d * 0.02, base + d * L, r), AMETHYST if i % 2 == 0 else LILAC, "head",
              fuse=False, glow=AMETHYST if i % 2 == 0 else LILAC, surface="shell")
    # a second, shorter row behind, so the back camera sees a full crown
    for i, deg in enumerate((-44, -16, 12, 40)):
        th = math.radians(deg)
        d = unit(math.sin(th) * 0.9, 0.45, math.cos(th))
        base = C + np.array([math.sin(th) * 0.08, 0.08, 0.02])
        k.add(f"crownb{i}", crystal(base - d * 0.02, base + d * 0.3, 0.05), LILAC if i % 2 == 0 else AMETHYST,
              "head", fuse=False, glow=LILAC if i % 2 == 0 else AMETHYST, surface="shell")
    # (2) the halo of light round the crown's foot, its front raised so that the game's low cameras see it as a ring
    k.add("halo", ring_band((0, 0.0, 0.775), unit(0, -0.55, 1), 0.13, 0.024, thick=0.02), HALO, "head",
          fuse=False, glow=HALO)

    # (3) the emblem on the upper belly, on the plate under its orange edge
    emblem(k, np.array([0.0, -0.186, 0.415]), unit(0, -0.95, 0.3), 0.056, "body")

    # (4) the clouds about the feet, clear of the feet, the thighs and the yellow paint low on the back
    avoid = [((0.175, -0.05, 0.04), (0.08, 0.1, 0.06)), ((-0.175, -0.05, 0.04), (0.08, 0.1, 0.06)),
             ((0.14, -0.01, 0.17), (0.1, 0.1, 0.1)), ((-0.14, -0.01, 0.17), (0.1, 0.1, 0.1)),
             ((0.165, -0.025, 0.1), (0.07, 0.07, 0.07)), ((-0.165, -0.025, 0.1), (0.07, 0.07, 0.07)),
             ((0, 0, 0.34), (0.23, 0.2, 0.225)), ((0, 0.13, 0.22), (0.16, 0.09, 0.1)),
             ((0.235, -0.14, 0.025), (0.05, 0.05, 0.03)), ((-0.235, -0.14, 0.025), (0.05, 0.05, 0.03))]
    clouds(k, 0.0, -0.01, 0.42, 0.19, 8, 0.082, avoid, seed=11, clear=0.026,
           skip=lambda x, y: abs(x) < 0.17 and y > 0.05)
    # a wisp curling beside each shoulder, outside the fists and under the crown's outer crystals
    wisp(k, "wispR", (0.47, 0.1, 0.92), 0.04, "body", n=4, toward=(-0.6, 0.3, 0.5))
    wisp(k, "wispL", (-0.47, 0.1, 0.9), 0.038, "body", n=4, toward=(0.6, 0.3, 0.5))


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("heracrossmega")
m.heracrossmega(*args())
