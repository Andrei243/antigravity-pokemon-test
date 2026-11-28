# Venusaur-Mega-Mmax: Venusaur-Mega's own script, whole and unchanged, with the Mega Dynamax form's additions laid
# on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venusaurmegammax.py -- <the export's Venusaur-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Venusaur.jpg), what it adds over the Mega,
# largest first: the five great petals turned to glowing pink-violet crystal (their white spots kept); a crown of eight
# tall violet and rose crystal shards with glowing orange tips standing from the flower's heart; the Key Stone's emblem
# glowing on the brow flower with a small shard over it; the clouds of Max energy rolling about the feet and a broken
# ring of crimson puffs floating at the petals' level, a few cyan curls on them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, slab, hull, srgb, unit, sibling, args, axes

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
PETALGLASS, SPOTGLASS = "#C870E6", "#F6DDFA"
SHARD_A, SHARD_B, TIP = "#9458E6", "#DA6ED8", "#FF8A3C"
DISC, RING, STAR = "#FF8CB4", "#FFD27A", "#FFFFFF"



def on_ell(c, r, n):
    """The point of an ellipsoid along a direction from its centre (bulbasaur.py's on_ell)."""
    n = np.array(n, float); n /= np.linalg.norm(n)
    return np.array(c) + n / np.sqrt(((n / np.array(r)) ** 2).sum())


def shard(k, name, root, d, length, r, col, tip, bone):
    """A six-sided crystal standing from root along d, its point glowing in the tip's colour."""
    k.add(name, lathe(root, d, [(0, 0), (r, 0), (r, 0.78 * length), (0, length)], seg=6), col, bone, fuse=False,
          glow=col)
    if tip:
        k.add(name + "tip", lathe(root, d, [(0, 0.76 * length), (r * 1.1, 0.76 * length), (r * 1.1, 0.8 * length),
                                            (0, length * 1.025)], seg=6), tip, bone, fuse=False, glow=tip)


def emblem(k, c, n, r, bone):
    """The Key Stone's emblem: a glowing disc in a ring of another colour, a four-pointed star on it."""
    n = unit(*n)
    k.add("emblem", lathe(c, n, [(0, 0), (r, 0), (r, 0.008), (0, 0.008)], seg=32), DISC, bone, fuse=False, glow=DISC)
    k.add("emblemring", ring_band(c + n * 0.004, n, r * 1.22, 0.012, thick=0.012), RING, bone, fuse=False, glow=RING)
    a, b = axes(n)
    pts = []
    for i in range(8):
        rr = r * (0.72 if i % 2 == 0 else 0.24)
        pts.append((rr * math.cos(math.pi * i / 4), rr * math.sin(math.pi * i / 4)))
    k.add("emblemstar", slab(pts, c + n * 0.011, a, b, 0.004), STAR, bone, fuse=False, glow=STAR)


def clump(k, name, at, size, col, other, bone, tangent=(1, 0, 0), rng=None, z0=None):
    """A cloud: three to five round puffs of mixed sizes, nearly as high as wide, one small one riding on top."""
    t = np.array(tangent, float); t /= np.linalg.norm(t)
    at = np.array(at, float)
    spec = [(0.0, 1.0, 0.0), (0.72, 0.8, -0.1), (-0.7, 0.72, -0.12), (1.3, 0.52, -0.25), (0.22, 0.55, 0.72)]
    for j, (o, s, h) in enumerate(spec):
        r = size * s * (1 + 0.12 * rng.uniform(-1, 1))
        p = at + t * o * size + np.array([0, 0, (r * 0.78 if z0 is None else 0) + h * size])
        if z0 is None:
            p[2] = max(p[2], r * 0.88)          # no puff under the ground
        else:
            p[2] = at[2] + h * size
        k.add(f"{name}_{j}", ell(p, (r, r, r * 0.88)), other if j == 2 else col, bone)


def ground_clouds(k, cx, cy, rx, ry, n, size, seed=7):
    rng = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        p = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), 0.0])
        tang = (-rx * math.sin(a), ry * math.cos(a), 0)
        clump(k, f"cloud{i}", p, size * (1 + 0.15 * math.sin(3 * a)), cols[i % 4], cols[(i + 1) % 4], "root", tang, rng)
        if i % 3 == 1:
            q = p + np.array([0.0, 0.0, size * 1.75])
            k.add(f"curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28), sub=2), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    TOP, H, HR = m.TOP, m.H, m.HR             # venusaurmega.py: the trunk's top; the head (venusaur.py's)
    # The petals turned to glowing crystal (the shape is the Mega's; only the colour and the light change)
    for p in k.parts:
        if p["name"].startswith("petal"):
            c = SPOTGLASS if "spot" in p["name"] else PETALGLASS
            p["color"], p["glow"] = srgb(c), c
    # The crown of crystal shards from the flower's heart, leaning a little out
    for i in range(8):
        a = 2 * math.pi * (i + 0.5) / 8
        o = np.array([math.sin(a), -math.cos(a), 0.0])
        root = TOP + o * 0.045 - np.array([0, 0, 0.03])
        length = 0.23 if i % 2 == 0 else 0.18
        shard(k, f"crown{i}x", root, o * 0.2 + np.array([0, 0, 1.0]), length, 0.024,
              SHARD_A if i % 2 == 0 else SHARD_B, TIP, "flower")
    shard(k, "crownmid", TOP - np.array([0, 0, 0.03]), (0, 0, 1), 0.26, 0.027, SHARD_A, TIP, "flower")
    # The emblem on the brow flower, standing off it, and a small shard over it
    n = unit(0, -0.5, 1)
    c = on_ell(H, HR, n) + n * 0.046
    emblem(k, c, n, 0.034, "head")
    shard(k, "browshard", c + n * 0.006 + unit(0, 0.6, 1) * 0.04, unit(0, 0.35, 1), 0.07, 0.016, SHARD_A, None,
          "head")
    # The clouds about the feet: within the Mega's own footprint (its ferns reach 0.68 a side)
    ground_clouds(k, 0.0, 0.03, 0.665, 0.665, 16, 0.076)
    # A broken ring of crimson puffs floating at the petals' level, the front over the face left clear
    rng = np.random.default_rng(11)
    for i, deg in enumerate((62, 150, 210, 298)):
        a = math.radians(deg)
        p = np.array([0.58 * math.sin(a), 0.08 - 0.58 * math.cos(a), 0.9 + 0.04 * (i % 2)])
        clump(k, f"float{i}", p, 0.058, (CRIMSON, DEEP)[i % 2], DEEP if i % 2 == 0 else CRIMSON, "flower",
              (math.cos(a), math.sin(a), 0), rng, z0=True)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        before = {p["name"] for p in self.parts}
        maxed(self)
        clearance(self, before)
    return _build(self, *a, **kw)
Kit.build = build


def clearance(k, before):
    """Prints how near each fused addition (a cloud) comes to the Mega's own fused body, point to point."""
    from mathutils.kdtree import KDTree
    body = [p for p in k.parts if p["name"] in before and p["fuse"] and not p["carve"]]
    pts = np.vstack([p["V"] for p in body])
    owner = np.concatenate([[j] * len(p["V"]) for j, p in enumerate(body)])
    kd = KDTree(len(pts))
    for i, v in enumerate(pts.tolist()):
        kd.insert(v, i)
    kd.balance()
    worst = {}
    for p in k.parts:
        if p["name"] in before or not p["fuse"] or p["carve"]:
            continue
        d, near = min((kd.find(v)[2], kd.find(v)[1]) for v in p["V"].tolist())
        g = p["name"].split("_")[0]
        if g not in worst or d < worst[g][0]:
            worst[g] = (d, body[owner[near]]["name"])
    least = sorted(worst.items(), key=lambda e: e[1][0])[:4]
    print("KIT-x1: clearance of the clouds from the body:", ", ".join(f"{g} {d:.3f} from {n}" for g, (d, n) in least))



m = sibling("venusaurmega")
m.venusaurmega(*args())
