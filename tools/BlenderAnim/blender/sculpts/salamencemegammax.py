# Salamence's Mega Dynamax form sculpted on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/salamencemegammax.py -- <the export's Salamence-Mega-Mmax.glb> <out.glb>
# Salamence-Mega's own script, whole and unchanged (salamencemega.py, called), with what the Mega Dynamax form lays over
# it added to its kit just before the body is meshed. From the picture (Mega-Dynamax-Salamence.jpg), largest first:
# the great red crescent turned to faceted ruby crystal, glowing, its top laid with facets of pink and deep red; streaks
# of red and orange crystal-fire trailing back and out from the crescent's outer edge on both wings; the clouds of Max
# energy, crimson and violet with cyan curls, in a ring on the ground under the feet, a small broken ring of crimson
# puffs over the head and a wisp by each shoulder; the Key Stone's emblem glowing on the breast, a pale disc in a red
# ring. The crescent's shape, the body, the face and the eyes are the Mega's.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
RUBY, RUBY_GLOW = "#D8324C", "#C42642"           # the crescent turned to crystal
PINK, DARK_FACET = "#F47A94", "#8E1A32"
FIRE_RED, FIRE_ORANGE = "#E8323A", "#F6782C"
STONE, STONE_RING = "#FFE0E8", "#FF3E5E"


def join(shapes):
    """Several closed shapes as one piece (one material a colour, as a glowing piece wants)."""
    V, F, off = [], [], 0
    for v, f in shapes:
        V.append(np.asarray(v, float)); F.extend([[i + off for i in t] for t in f]); off += len(v)
    return np.vstack(V), F


def clouds(k, cx, cy, rx, ry, n, size, seed, bone="root"):
    """The ground's ring of clouds: mounds of five round puffs of mixed sizes packed close (a big one, two lower at its
    sides, one behind, a small one riding on top), each mound one colour with one puff of another, a cyan curl on
    every other. The puffs are loose pieces: fused, the mounds took the build past 60,000 vertices."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP)
    alt = (VIOLET, DVIOLET, CRIMSON)
    PUFFS = ((1.0, 0.0, 1.0, 0.95), (0.74, 0.2, 1.02, 0.9), (0.7, -0.2, 0.99, 0.9), (0.72, 0.04, 1.12, 0.9),
             (0.5, -0.03, 0.98, 1.75))
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        for j, (f, da, sc, zf) in enumerate(PUFFS):
            s = size * f * (1 + 0.1 * rnd.uniform(-1, 1))
            t = a + da * 2 * math.pi / n
            p = np.array([cx + rx * sc * math.cos(t), cy + ry * sc * math.sin(t), s * zf])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.88), sub=3 if j < 4 else 2),
                  alt[i % 3] if j == (1 if i % 2 else 2) else cols[i % 3], bone, fuse=False)
        if i % 2 == 0:
            q = np.array([cx + rx * 1.06 * math.cos(a + 0.12), cy + ry * 1.06 * math.sin(a + 0.12), size * 1.45])
            curls.append(ell(q, (size * 0.3, size * 0.3, size * 0.24), sub=3))
    k.add("curls", join(curls), CYAN, bone, fuse=False, glow=CYAN)


def board(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    h, w, l = V[:, 2].max(), np.ptp(V[:, 0]), np.ptp(V[:, 1])
    a = math.radians(31.5)
    proj = V[:, 0] * math.cos(a) + V[:, 1] * math.sin(a)
    print("KIT: board: corners %.3f (0.853 w + 0.522 l), shows %.3f, in a square of %.3f"
          % (0.853 * w + 0.522 * l, np.ptp(proj), 1.3 * max(h, 0.85 * max(w, l))))


def maxed(k):
    """What the Mega Dynamax form lays over Salamence-Mega: called with the Mega's kit just before it is meshed."""
    # 1. The crescent turned to ruby crystal: the Mega's own sheet and rim recoloured and lit, facets laid on its top
    for p in k.parts:
        if p["name"] == "crescent":
            p["color"] = sculptkit.srgb(RUBY); p["glow"] = RUBY_GLOW
        elif p["name"] == "crescentrim":
            p["color"] = sculptkit.srgb(DARK_FACET)

    def frame(u, v):
        e = 1e-3
        P = m.cres_at(np.array([[u]]), np.array([[v]]))[0, 0]
        du = m.cres_at(np.array([[u + e]]), np.array([[v]]))[0, 0] - P
        dv = m.cres_at(np.array([[u]]), np.array([[v + e]]))[0, 0] - P
        nrm = np.cross(du, dv); nrm /= np.linalg.norm(nrm)
        if nrm[2] < 0:
            nrm = -nrm
        return P, nrm

    rnd = np.random.default_rng(11)
    pinks, darks = [], []
    us = np.linspace(0.06, 0.94, 21)
    for i, u in enumerate(us):
        for j, v in enumerate((0.25, 0.5, 0.75)):
            uu = u + (0.01, -0.008, 0.012)[j]
            cnr = []
            for du, dv in ((-0.019, -0.1), (0.02, -0.07 + 0.04 * rnd.uniform()), (0.0, 0.11)) if (i + j) % 2 else \
                    ((0.019, -0.1), (-0.02, 0.07), (0.017, 0.11)):
                if not 0.03 < uu + du < 0.97:
                    continue
                cnr.append((uu + du, min(max(v + dv, 0.06), 0.94)))
            if len(cnr) < 3:
                continue
            top, bot = [], []
            for a, b in cnr:
                P, nrm = frame(a, b)
                top.append(P + nrm * 0.0215)
                bot.append(P + nrm * 0.0155)
            (pinks if (i + j) % 3 == 0 else darks if (i + j) % 3 == 1 else pinks).append(hull(top + bot))
    k.add("facetspink", join(pinks), PINK, "wingR", fuse=False, glow=PINK, surface="shell")
    k.add("facetsdark", join(darks), DARK_FACET, "wingR", fuse=False, surface="shell")

    # 2. Streaks of crystal-fire trailing back and out from the crescent's outer edge, on both wings
    reds, oranges = [], []
    for side in (-1, 1):
        for i, (u, L, r) in enumerate(((0.56, 0.22, 0.026), (0.62, 0.3, 0.03), (0.68, 0.36, 0.032), (0.74, 0.32, 0.03), (0.8, 0.28, 0.028),
                                       (0.86, 0.2, 0.024))):
            uu = u if side > 0 else 1 - u
            P = m.cres_at(np.array([[uu]]), np.array([[0.93]]))[0, 0]
            Q = m.cres_at(np.array([[uu]]), np.array([[0.5]]))[0, 0]
            out_ = P - Q; out_[2] = 0; out_ /= np.linalg.norm(out_)
            d = out_ * 0.3 + np.array([0, 0.85, -0.3]); d /= np.linalg.norm(d)
            a1 = np.cross(d, (0, 0, 1.0)); a1 /= np.linalg.norm(a1)
            a2 = np.cross(a1, d)
            root = P - d * 0.02
            tip = root + d * L
            pts = [root + a1 * r, root - a1 * r, root + a2 * r * 0.62, root - a2 * r * 0.62,
                   root + d * L * 0.4 + a1 * r * 0.8, root + d * L * 0.4 - a1 * r * 0.8, tip]
            (oranges if i % 2 else reds).append(hull(pts))
    k.add("firered", join(reds), FIRE_RED, "wingR", fuse=False, glow=FIRE_RED, surface="shell")
    k.add("fireorange", join(oranges), FIRE_ORANGE, "wingR", fuse=False, glow=FIRE_ORANGE, surface="shell")

    # 3. The clouds: a ring under the feet, clear of feet and tail; a broken ring over the head; a wisp at each shoulder
    clouds(k, 0.0, 0.02, 0.3, 0.54, 8, 0.08, 7)
    top = m.HC + np.array([0.0, 0.03, 0.29])
    for i, a in enumerate((20, 75, 140, 200, 255, 320)):
        t = math.radians(a)
        p = top + np.array([0.1 * math.cos(t), 0.08 * math.sin(t), 0.008 * math.sin(3 * t)])
        s = (0.026, 0.02, 0.024, 0.018, 0.025, 0.02)[i]
        k.add(f"halo{i}", ell(p, (s, s, s * 0.85)), CRIMSON if i % 3 else DEEP, "head")
        k.add(f"halob{i}", ell(p + np.array([0.012 * math.cos(t + 1), 0.012 * math.sin(t + 1), 0.006]),
                                (s * 0.7, s * 0.7, s * 0.6)), CRIMSON, "head")
    for s, side in ((1, "R"), (-1, "L")):
        c = np.array([0.17 * s, -0.19, 0.44])
        k.add(f"wisp{side}0", ell(c, (0.032, 0.03, 0.028), sub=3), VIOLET if s > 0 else CRIMSON, "body", fuse=False)
        k.add(f"wisp{side}1", ell(c + np.array([0.03 * s, 0.012, 0.022]), (0.022, 0.02, 0.02), sub=3),
              CRIMSON if s > 0 else VIOLET, "body", fuse=False)

    # 4. The Key Stone's emblem on the breast: a pale glowing disc in a red ring, a hair off the chest
    c = np.array([0.0, -0.243, 0.272])
    d = unit(0, -1, -0.18)
    k.add("emblem", lathe(c, d, [(0, 0), (0.026, 0), (0.026, 0.008), (0, 0.008)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + d * 0.004, d, 0.034, 0.01, thick=0.008), STONE_RING, "body", fuse=False,
          glow=STONE_RING)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    first = not _laid
    if first:                   # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    ob = _build(self, *a, **kw)
    if first:
        board(ob)
    return ob
Kit.build = build

m = sibling("salamencemega")
m.salamence_mega(*args())
