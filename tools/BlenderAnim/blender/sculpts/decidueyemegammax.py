# Mega Dynamax Decidueye (a form of our own, plan 06 · R20c) on the skeleton and clips of the game's own export of the
# form (sculptkit.py): Decidueye-Mega's own script, whole and unchanged (sibling("decidueyemega")), with the form's
# additions laid on its kit just before it is meshed:
#   blender --background --factory-startup --python sculpts/decidueyemegammax.py -- <the export's Decidueye-Mega-Mmax.glb> <out.glb>
# The design is docs/art/reference/gemini/megadynamax/Mega-Dynamax-Decidueye.jpg: a long arrow of teal light nocked and
# laid across the Mega's bow, its great crystal head swept out past the cloak (its largest addition, carried to the +X
# side the game's front camera sees), the Key Stone's emblem on the chest under the bow with four teal crystal points
# round it, clouds of Max energy rolling about the feet and the quills, and a wisp of smoke rising behind a shoulder.
# The sculptor's list (PokemonModels.MegaMax1.cs, Decidueye) is the same; its bow upright at the side is the Mega's
# own bow here, which lies across the chest.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, ring_band, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#4EE2EC"
TEAL, TEALPALE = "#3FE3CC", "#B4FAF0"                     # the Mega's own teal light (decidueyemega.py)
STONE, RING = "#F2C24A", "#3FE3CC"                        # the emblem: a gold disc in a teal ring, as the picture's


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


def clouds(k, c, rx, ry, H, n, keep, gap, seed, phase=0.0, most=1.3):
    """A ring of fused mounds on the ground: clumps of four round puffs, each pushed out until it stands clear of keep
    (a puff that would have to go further out than `most` times the ring is left out)."""
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
            ok = False
            while f <= most:
                p = np.array([c[0] + rx * f * math.cos(t), c[1] + ry * f * math.sin(t), z])
                if keep_clear(p, s, keep) >= gap:
                    ok = True
                    break
                f += 0.02
            if not ok:
                continue
            col = cols[i % 3] if j != 2 else cols[(i + 1) % 3]
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9), sub=3), col, "root")
            if j == 0 and i % 3 == 0:
                q = p + np.array([0.0, 0.0, s * 1.0]) + np.array([math.cos(t), math.sin(t), 0]) * s * 0.5
                k.add(f"curl{i}", outward(ell(q, (s * 0.42, s * 0.42, s * 0.36), sub=3)), CYAN, "root", fuse=False,
                      glow=CYAN)


def front_at(k, x, z, near=0.012):
    """How far forward (least y) the fused parts reach at (x, z): where a piece laid on the front stands."""
    best = None
    for p in k.parts:
        if not p["fuse"] or p["carve"]:
            continue
        V = np.asarray(p["V"])
        sel = V[(np.abs(V[:, 0] - x) < near) & (np.abs(V[:, 2] - z) < near)]
        if len(sel):
            y = float(sel[:, 1].min())
            best = y if best is None else min(best, y)
    return best


def emblem(k, c, d, r, bone):
    """The Key Stone's emblem: a glowing disc in a glowing ring, a hair off the body."""
    k.add("emblem", lathe(c, d, [(0, 0), (r, 0), (r, 0.006), (0, 0.006)]), STONE, bone, fuse=False, glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.003, d, r * 1.25, 0.008, thick=r * 0.32), RING, bone, fuse=False,
          glow=RING)


def maxed(k):
    H = 0.75
    # (a) the arrow of light: a shaft from the nock by the -X end of the bow across it and out past the cloak on +X,
    # a great crystal head and two fletchings
    nock = np.array([-0.2, -0.17, 0.44])
    head0 = np.array([0.25, -0.25, 0.41])
    k.add("arrow", outward(tube([nock, (nock + head0) / 2, head0], [0.012, 0.012, 0.012])), TEAL, "body", fuse=False,
          glow=TEAL)
    du = unit(*(head0 - nock))
    up = np.array([0, 0, 1.0]) - du * du[2]; up /= np.linalg.norm(up)
    side = np.cross(du, up)
    k.add("arrowhead", hull([head0 - du * 0.02 + up * 0.05, head0 - du * 0.02 - up * 0.05,
                             head0 + side * 0.012, head0 - side * 0.012, head0 + du * 0.12,
                             head0 + du * 0.03 + up * 0.03 + side * 0.008, head0 + du * 0.03 - up * 0.03 - side * 0.008]),
          TEALPALE, "body", fuse=False, glow=TEALPALE)
    for sgn in (1, -1):
        out = [(0.0, 0.0), (0.06, 0.0), (0.075, 0.03 * sgn), (0.02, 0.034 * sgn)]
        k.add(f"fletch{'RL'[sgn < 0]}", slab(out, nock + du * 0.005, du, up, 0.012), TEAL, "body", fuse=False,
              glow=TEAL)
    # (c) the emblem on the chest under the bow, a teal crystal point at each quarter round it
    y = front_at(k, 0.0, 0.33)
    c = np.array([0.0, (y if y is not None else -0.06) - 0.002, 0.33])
    d = np.array([0.0, -1.0, 0.0])
    r = 0.022
    emblem(k, c, d, r, "body")
    for i, (ex, ez) in enumerate(((0, 1), (1, 0), (0, -1), (-1, 0))):
        e = np.array([ex, 0.0, ez])
        b = c + e * (r * 1.25 + 0.012) + d * 0.004
        L = 0.034 if ez else 0.026
        k.add(f"emblempoint{i}", hull([b + np.cross(e, d) * 0.009, b - np.cross(e, d) * 0.009, b + d * 0.007,
                                       b - d * 0.002, b + e * L]), TEALPALE, "body", fuse=False, glow=TEALPALE)
    # (b) the clouds about the feet, the cloak's hem and the quills, kept clear of them all
    pts, rads = [], []
    for sx in (1, -1):
        for P, R in (((0.104, 0.024, 0.06), 0.07), ((0.104, 0.11, 0.04), 0.05), ((0.104, -0.06, 0.04), 0.05),
                     ((0.06, -0.06, 0.02), 0.04), ((0.05, -0.02, 0.02), 0.04), ((0.13, 0.03, 0.03), 0.06)):
            pts.append(np.array(P) * np.array([sx, 1, 1])); rads.append(R)
    for i in range(7):
        x = -0.15 + 0.05 * i
        root = np.array([x * 0.8, 0.12 + 0.06 * abs(x), 0.1])
        tip = np.array([x * 1.7, 0.33 - 0.1 * abs(x), 0.025])
        for u in np.linspace(0, 1.15, 10):
            pts.append(root + (tip - root) * u); rads.append(0.022)
    clouds(k, (0.0, 0.03), 0.27, 0.29, H, 9, (np.array(pts), np.array(rads)), 0.02, 3, phase=-0.17)
    # a wisp of smoke rising behind the -X shoulder, as the picture's plume
    for j in range(4):
        q = np.array([-0.17 - 0.015 * j, 0.06 + 0.01 * j, 0.42 + 0.05 * j])
        k.add(f"wisp{j}", outward(ell(q, (0.03 - 0.004 * j,) * 3, sub=3)), (CRIMSON, DEEP, VIOLET, CRIMSON)[j], "body",
              fuse=False)


m = sibling("decidueyemega")
_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m.decidueyemega(*args())
