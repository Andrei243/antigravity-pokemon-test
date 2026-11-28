# Mega Dynamax Serperior (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py): Serperior-Mega's own script, whole and unchanged (sibling("serperiormega")), with the form's
# additions laid on its kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/serperiormegammax.py -- <the export's Serperior-Mega-Mmax.glb> <out.glb>
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Serperior.jpg: the leaf wings' outer feathers grown
# into glowing violet crystal fanning up and out from each spar (its largest addition), the hood's leaves tipped with
# the same crystal, the Key Stone's emblem on the brow, clouds of Max energy rolling about the coils and a broken ring
# of crimson puffs over the head with a wisp by the neck (the sculptor's list, PokemonModels.MegaMax2.cs, SerperiorMmax).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, lathe, ring_band, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
CRYSTAL, CRYSTAL2 = "#9A62E6", "#B488F2"                  # the picture's lavender crystal, two tones by turns
STONE, RING = "#F0A0FF", "#A060F0"                        # the emblem: a pink-lilac disc in a violet ring


def keep_clear(c, s, keep):
    """How far a puff of radius s at c stands off the nearest of the parts it must not touch (points with radii)."""
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


def puffs(k, name, centre, radius, n, size, bone, gap_at=None, z_wave=0.0, col=CRIMSON):
    """A broken ring of loose crimson puffs, level, about centre; gap_at leaves one place empty."""
    for i in range(n):
        a = 2 * math.pi * i / n
        if gap_at is not None and abs((a - gap_at + math.pi) % (2 * math.pi) - math.pi) < math.pi / n:
            continue
        s = size * (1.0 if i % 2 == 0 else 0.78)
        p = np.array(centre) + np.array([radius * math.cos(a), radius * math.sin(a), z_wave * math.sin(3 * a)])
        k.add(f"{name}{i}", outward(ell(p, (s, s, s * 0.85), sub=3)), col if i % 3 else DEEP, bone, fuse=False)


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a glowing disc in a glowing ring, a hair off the body."""
    k.add("emblem", lathe(c, d, [(0, 0), (r, 0), (r, 0.006), (0, 0.006)]), STONE, bone, fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.003, d, r * 1.25, 0.008, thick=r * 0.32), RING, bone, fuse=False,
          glow=RING)


def maxed(k, m):
    se = m.se
    H = 0.75
    # (a) the wings' crystal: a fan of long violet crystal feathers from each spar, up and out, faces to front and back
    for s, side in ((1, "R"), (-1, "L")):
        root = np.array([0.03 * s, 0.01, 0.36])
        spar = np.array([0.24 * s, 0.1, 0.6])
        face = (-0.25 * s, -1.0, 0.0)
        for i, (t, ang, L) in enumerate(((0.55, 12, 0.13), (0.7, 30, 0.15), (0.85, 48, 0.16), (1.0, 66, 0.15),
                                          (1.0, 86, 0.12))):
            at = root + (spar - root) * t + np.array([0, 0.012, 0])
            a = math.radians(ang)
            dd = np.array([math.sin(a) * s, 0.12, math.cos(a)])
            dd /= np.linalg.norm(dd)
            k.add(f"crystal{side}{i}", m.leaf_plate(at, at + dd * L, 0.032, face, thick=0.022),
                  CRYSTAL if i % 2 == 0 else CRYSTAL2, "seg12", fuse=False, glow=CRYSTAL if i % 2 == 0 else CRYSTAL2)
        # the wing tip's leaf carried on into crystal
        tip0 = spar + np.array([0.08 * s, 0.03, 0.07])
        dd = unit(0.75 * s, 0.25, 0.62)
        k.add(f"crystaltip{side}", m.leaf_plate(tip0 - dd * 0.01, tip0 + dd * 0.09, 0.028, face, thick=0.022), CRYSTAL2,
              "seg12", fuse=False, glow=CRYSTAL2)
    # the hood's upper leaves tipped with crystal, carried on past their points
    for i in range(1, 10):
        a = math.radians(-115 + 23 * i)
        rad = np.array([math.sin(a), 0.0, math.cos(a) * 0.9])
        rad /= np.linalg.norm(rad)
        L = 0.17 - 0.025 * abs(math.sin(a * 0.5))
        tip = m.HOOD + rad * (0.06 + L) + (0, -0.035, 0)
        k.add(f"hoodcrystal{i}", m.leaf_plate(tip - rad * 0.015 + (0, 0.006, 0), tip + rad * 0.07 + (0, -0.01, 0),
                                              0.026, (0, -1, 0), thick=0.02), CRYSTAL if i % 2 else CRYSTAL2, "head",
              fuse=False, glow=CRYSTAL if i % 2 else CRYSTAL2)
    # (c) the emblem on the brow, between and above the eyes, faced up and forward to the front camera
    d = unit(0.0, -1.0, 0.8)
    HC, HR = se.HC, se.HR
    t = 1.0 / math.sqrt(float(np.sum((d / HR) ** 2)))
    emblem(k, HC + d * (t + 0.004), d, 0.026, "head")
    # (b) the clouds about the coils, kept clear of the body and tail
    pts, rads = [], []
    line = np.array(se.LINE)
    for i in range(len(line) - 1):
        for u in np.linspace(0, 1, 6, endpoint=False):
            pts.append(line[i] + (line[i + 1] - line[i]) * u)
            rads.append(se.RADS[i] + (se.RADS[i + 1] - se.RADS[i]) * u)
    keep = (np.array(pts), np.array(rads))
    clouds(k, (0.03, 0.06), 0.33, 0.31, H, 11, keep, 0.02, 11, phase=0.14)
    # a broken ring of crimson puffs over the head and a wisp by the neck
    puffs(k, "crown", HC + np.array([0.0, 0.06, 0.24]), 0.075, 7, 0.022, "head", gap_at=-math.pi / 2, z_wave=0.008)
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.135 * s - 0.02, -0.04, 0.33 + (0.04 if s > 0 else 0.0)])
        for j in range(3):
            q = c + np.array([0.02 * j * s, 0.012 * j, 0.022 * j])
            k.add(f"wisp{side}{j}", outward(ell(q, (0.022 - 0.004 * j,) * 3, sub=3)), (CRIMSON, DEEP, VIOLET)[j],
                  "seg12", fuse=False)


m = sibling("serperiormega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self, m)
    return _build(self, *a, **kw)
Kit.build = build

m.serperiormega(*args())
