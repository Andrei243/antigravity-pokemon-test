# Blastoise-Mega-Mmax: Blastoise-Mega's own script, whole and unchanged, with the Mega Dynamax form's additions laid
# on its kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blastoisemegammax.py -- <the export's Blastoise-Mega-Mmax.glb> <out.glb>
#
# From the design (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Blastoise.jpg), what it adds over the Mega,
# largest first: the cannons turned to six-sided tubes of glowing cyan crystal full of water, the great cannon sheathed
# in a broad one from its middle to past its muzzle and two more standing from the shell on short silver collars, one
# leaning forward over the right shoulder and one upright behind the left; the forearm cannons sheathed the same; the
# Key Stone's emblem glowing on the upper belly; the clouds of Max energy rolling about the feet and a broken ring of
# crimson puffs about the shoulders, a few cyan curls on them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
import sculptkit
from sculptkit import Kit, ell, lathe, ring_band, slab, srgb, unit, sibling, args, axes

CRIMSON, DEEP, VIOLET, DEEPV, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
GLASS, WATER, FOAM, METAL = "#62E0EE", "#2C8ED0", "#A6EEF6", "#C4C8D2"
DISC, RING, STAR = "#5EC8E8", "#FF7CC8", "#FFF6D6"


def crystal_tube(k, name, base, d, length, r, bone, mirror=False):
    """A six-sided tube of crystal from base along d, open at its far end on a bore of water."""
    ri, depth = r * 0.7, min(0.05, length * 0.3)
    k.add(name, lathe(base, d, [(0, 0), (r, 0), (r, length), (ri, length), (ri, length - depth), (0, length - depth)],
                      seg=6), GLASS, bone, fuse=False, glow=GLASS, mirror=mirror)
    k.add(name + "water", lathe(base + np.array(d) * (length - depth + 0.002), d,
                                [(0, 0), (ri * 0.98, 0), (ri * 0.98, 0.004), (0, 0.004)], seg=6),
          WATER, bone, fuse=False, glow=WATER, mirror=mirror)
    # a pale band of foam round the tube near its mouth
    k.add(name + "foam", lathe(base + np.array(d) * (length * 0.62), d,
                               [(0, 0), (r * 1.06, 0), (r * 1.06, length * 0.1), (0, length * 0.1)], seg=6),
          FOAM, bone, fuse=False, glow=FOAM, mirror=mirror)


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
    """A cloud: four or five round puffs of mixed sizes, nearly as high as wide, one small one riding on top."""
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


def ground_clouds(k, cx, cy, rx, ry, n, size, avoid=(), seed=7):
    rng = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DEEPV)
    for i in range(n):
        a = 2 * math.pi * (i + 0.25) / n
        p = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a), 0.0])
        if any(np.hypot(p[0] - q[0], p[1] - q[1]) < q[2] for q in avoid):
            continue
        tang = (-rx * math.sin(a), ry * math.cos(a), 0)
        clump(k, f"cloud{i}", p, size * (1 + 0.15 * math.sin(3 * a)), cols[i % 4], cols[(i + 1) % 4], "root", tang, rng)
        if i % 3 == 1:
            q = p + np.array([0.0, 0.0, size * 1.75])
            k.add(f"curl{i}", ell(q, (size * 0.32, size * 0.32, size * 0.28), sub=2), CYAN, "root", fuse=False, glow=CYAN)


def maxed(k):
    """What the Mega Dynamax form lays over its Mega: called with the Mega's kit just before it is meshed."""
    # The great cannon sheathed in crystal from its middle past its muzzle (blastoisemega.py: GB, GD, GL, GR)
    crystal_tube(k, "gcrystal", m.GB + m.GD * 0.3, m.GD, m.GL - 0.3 + 0.05, m.GR * 1.42, "body")
    # Two more on the shell, each on a short silver collar sunk into it
    for name, root, d, L, r in (("tubeR", (0.17, 0.12, 0.8), unit(0.35, -0.5, 0.8), 0.32, 0.062),
                                ("tubeL", (-0.17, 0.22, 0.72), unit(-0.22, 0.28, 1), 0.33, 0.066)):
        root = np.array(root)
        k.add(name + "collar", lathe(root - d * 0.04, d, [(0, 0), (r * 0.82, 0), (r * 0.82, 0.11), (0, 0.11)], seg=32),
              METAL, "body", fuse=False, surface="metal")
        crystal_tube(k, name, root + d * 0.07, d, L, r, "body")
    # The forearm cannons sheathed the same (blastoisemega.py's forearms: cb = cc + (0.025, 0.06, 0.07), FD, L 0.29)
    cc = (m.ELBOW + m.WRIST) / 2 + np.array([0.008, 0.0, 0.012])
    cb = cc + np.array([0.025, 0.06, 0.07])
    crystal_tube(k, "acrystal", cb + m.FD * 0.12, m.FD, 0.2, 0.064, "arm{s}", mirror=True)
    # The emblem on the upper belly (the torso's front, blastoise.py's TC, TR), standing off it
    n = unit(0, -1, 0.15)
    c = m.TC + np.array([0, -m.TR[1] - 0.012, 0.06])
    emblem(k, c, n, 0.052, "body")
    # The clouds about the feet, clear of the feet and of the tail's tip
    ground_clouds(k, 0.0, -0.06, 0.445, 0.445, 12, 0.066, avoid=[(0.13, 0.41, 0.17)])
    # A broken ring of crimson puffs about the shoulders and the shell, the front left clear
    rng = np.random.default_rng(11)
    for i, deg in enumerate((70, 145, 255, 300)):
        a = math.radians(deg)
        p = np.array([0.4 * math.sin(a), 0.05 - 0.36 * math.cos(a), 0.86 + 0.06 * (i % 2)])
        clump(k, f"float{i}", p, 0.048, (CRIMSON, DEEP)[i % 2], DEEP if i % 2 == 0 else CRIMSON, "body",
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



m = sibling("blastoisemega")
m.blastoisemega(*args())
