# Mega Dynamax Blaziken (a form of this project's own) as Mega Blaziken's own sculpt (blazikenmega.py), whole and
# unchanged, with what the form lays over it put on the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/blazikenmegammax.py -- <the export's Blaziken-Mega-Mmax.glb> <out.glb>
# What the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Blaziken.jpg) adds to the Mega, largest first:
#   1. each fist wrapped in great flames of crystal: a crown of faceted orange and gold shards about the fist and wrist,
#      up, out and back, the knuckles left showing in front, a long ribbon of flame streaming on from each;
#   2. the clouds of Max energy: fused mounds about the feet, crimson and violet by turns with cyan curls, a broken ring
#      of crimson puffs floating behind the plumes over the head and a wisp over each shoulder;
#   3. the Key Stone's emblem on the cream bib: a glowing orange disc in a ring of gold.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, lathe, ring_band, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
FIRE, GOLD, AMBER = "#FF7A2A", "#FFD25A", "#FFA544"      # the crystal flames
STONE, RINGC = "#FF8A2E", "#FFE07A"                       # the emblem

# From blazikenmega.py: the arm's shoulder, elbow and wrist (+X side), and the palm a little past the wrist
SH, EL, WR = np.array([0.09, -0.01, 0.72]), np.array([0.168, 0.015, 0.6]), np.array([0.155, -0.15, 0.565])
ALONG = (WR - EL) / np.linalg.norm(WR - EL)
PALM = WR + ALONG * 0.035


def join(*shapes):
    """Several closed shapes as one piece: a glowing piece is a material of its own, so like pieces go as one."""
    Vs, Fs, n = [], [], 0
    for V, F in shapes:
        Vs.append(np.asarray(V)); Fs += [[int(i) + n for i in f] for f in F]; n += len(V)
    return np.vstack(Vs), Fs


def shard(root, d, length, width, thick):
    """A flame of crystal: a flat-faced kite from root along d, widest a third of the way, to a point."""
    d = unit(*d)
    a = np.cross(d, (0, 0, 1.0)); a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-3 else np.array([1.0, 0, 0])
    b = np.cross(d, a)
    m = root + d * length * 0.32
    return hull([root + a * width * 0.6, root - a * width * 0.6, root + b * thick * 0.6, root - b * thick * 0.6,
                 m + a * width, m - a * width, m + b * thick, m - b * thick, root + d * length])


def clouds(k, cx, cy, rx, ry, n, size, seed):
    """The clouds of Max energy on the ground: n clumps of round puffs in a ring about (cx, cy), fused on the root bone
    (islands of their own), each in one colour with one puff of another, a few cyan curls riding on them."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        col = cols[i % 4]
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.16, -0.15, 0.03)[j] * (0.33 / max(rx, ry))
            g = (0.0, 0.03, 0.025, -0.01)[j]
            p = np.array([cx + (rx + g) * math.cos(t), cy + (ry + g) * math.sin(t), s * (0.93, 0.95, 0.93, 2.1)[j]])
            k.add(f"cloud{i}_{j}", ell(p, (s, s, s * 0.9)), col if j != 2 else cols[(i + 1) % 4], "root")
        if i % 3 == 0:
            q = np.array([cx + (rx + 0.03) * math.cos(a + 0.05), cy + (ry + 0.03) * math.sin(a + 0.05), size * 1.55])
            curls.append(ell(q, (size * 0.32, size * 0.32, size * 0.28), sub=3))
    k.add("curls", join(*curls), CYAN, "root", fuse=False, glow=CYAN)


def puffs(name, centre, radius, n, size, gap, bone, k, seed, lift=0.0):
    """A broken ring of crimson puffs floating level about centre, loose, its gap at the front."""
    rnd = np.random.default_rng(seed)
    shapes = []
    for i in range(n):
        a = math.radians(-90 + gap / 2 + (360 - gap) * i / (n - 1))
        s = size * (0.8 + 0.4 * rnd.uniform())
        p = np.array(centre) + np.array([radius * math.cos(a), radius * math.sin(a), lift * math.sin(2 * a)])
        shapes.append(ell(p, (s, s, s * 0.85), sub=3))
    k.add(name, join(*shapes), CRIMSON, bone, fuse=False)


def wisp(name, pts, sizes, k, bone, colour=CRIMSON):
    """A curl of cloud by a shoulder: a few loose puffs along a short rising path."""
    k.add(name, join(*[ell(p, (s, s, s * 0.9), sub=3) for p, s in zip(pts, sizes)]), colour, bone, fuse=False)


def crystal_fist(k):
    """Crystal flames about each fist (authored on +X, mirrored): shards from round the palm and wrist, up, out and back,
    none across the knuckles' front, in three glowing colours."""
    out = np.array([1.0, 0, 0])
    fire, gold, amber = [], [], []
    # (where round the palm: across, back, up), (way), length, width
    for i, (o, d, ln, w) in enumerate([
            ((0.035, 0.01, 0.02), (0.55, 0.25, 0.8), 0.22, 0.05),
            ((0.03, 0.03, -0.01), (0.9, 0.35, 0.25), 0.18, 0.045),
            ((0.0, 0.035, 0.03), (0.05, 0.45, 0.9), 0.24, 0.052),
            ((-0.03, 0.02, 0.025), (-0.45, 0.35, 0.85), 0.17, 0.042),
            ((0.03, -0.02, -0.03), (0.75, -0.1, -0.35), 0.13, 0.04),
            ((0.0, 0.05, -0.02), (0.2, 0.95, 0.15), 0.16, 0.042),
            ((0.02, 0.07, 0.03), (0.45, 0.7, 0.65), 0.2, 0.046),
            ((-0.035, -0.005, -0.02), (-0.7, 0.1, -0.3), 0.11, 0.036)]):
        root = PALM + np.array(o) + (WR - PALM) * 0.3
        s = shard(root, d, ln * 1.3, w * 1.15, 0.03)
        (fire, gold, fire)[i % 3].append(s)
    # a faceted block of crystal over the wrist and the back of the hand, the knuckles and fingers out of it in front
    a = np.cross(ALONG, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(a, ALONG)
    rnd = np.random.default_rng(5)
    c = WR + ALONG * 0.012 + np.array([0, 0, 0.022])
    pts = []
    for _ in range(22):
        v = rnd.normal(size=3); v /= np.linalg.norm(v)
        v[2] = min(v[2], 0.55)                              # its front is cut flat, short of the knuckles
        pts.append(c + a * v[0] * 0.066 + b * v[1] * 0.06 + ALONG * v[2] * 0.08)
    amber.append(hull(pts))
    # a cuff of short shards round the wrist, rising
    for j in range(6):
        t = 2 * math.pi * j / 6
        a = np.cross(ALONG, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(ALONG, a)
        r = a * math.cos(t) + b * math.sin(t)
        root = WR - ALONG * 0.01 + r * 0.032
        gold.append(shard(root, r + np.array([0, 0.3, 0.9]), 0.1, 0.03, 0.022))
    k.add("crystalfire", join(*fire), FIRE, "arm{s}", fuse=False, mirror=True, glow=FIRE)
    k.add("crystalgold", join(*gold), GOLD, "arm{s}", fuse=False, mirror=True, glow=GOLD)
    k.add("crystalamber", join(*amber), AMBER, "arm{s}", fuse=False, mirror=True, glow=AMBER)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Blaziken: called with the Mega's kit just before it is meshed."""
    crystal_fist(k)
    # The emblem on the cream bib, a hair off its front (the Mega's bib part)
    bib = next(p for p in k.parts if p["name"] == "bib")
    V = bib["V"]
    zc = 0.695
    sel = V[(np.abs(V[:, 0]) < 0.015) & (np.abs(V[:, 2] - zc) < 0.02)]
    n = unit(0.0, -1, 0.15)
    c = np.array([0.0, sel[:, 1].min() - 0.004, zc])
    k.add("emblemstone", lathe(c, n, [(0, 0), (0.036, 0), (0.036, 0.01), (0, 0.014)]), STONE, "body", fuse=False,
          glow=STONE)
    k.add("emblemring", ring_band(c + n * 0.006, n, 0.042, 0.012, thick=0.012), RINGC, "body", fuse=False, glow=RINGC)
    # The clouds: mounds about the feet (the boots' hems reach x 0.21, the claws y -0.13), clear of them all round
    clouds(k, 0.0, -0.01, 0.31, 0.29, 10, 0.068, 11)
    # A broken ring of crimson puffs floating behind the plumes over the head; a wisp over each shoulder
    puffs("headring", (0.0, 0.04, 1.1), 0.2, 18, 0.034, 70, "head", k, 3, lift=0.015)
    wisp("wispR", [(0.15, 0.05, 0.76), (0.2, 0.07, 0.81), (0.23, 0.05, 0.87)], (0.032, 0.026, 0.02), k, "body")
    wisp("wispL", [(-0.15, 0.05, 0.77), (-0.19, 0.08, 0.83)], (0.028, 0.022), k, "body", VIOLET)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("blazikenmega")
m.blazikenmega(*args())
