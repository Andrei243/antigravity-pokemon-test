# Mega Dynamax Sceptile (a form of this project's own) as Mega Sceptile's own sculpt (sceptilemega.py), whole and
# unchanged, with what the form lays over it put on the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/sceptilemegammax.py -- <the export's Sceptile-Mega-Mmax.glb> <out.glb>
# What the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Sceptile.jpg) adds to the Mega, largest first:
#   1. a whole tree grown from the end of the tail: a dark trunk and branches, a crown of glowing green leaves as large
#      as the body's trunk, reaching up beside the head;
#   2. the clouds of Max energy: fused mounds about the feet, crimson and violet by turns with cyan curls, a broken
#      ring of crimson puffs floating over the head and a wisp over each shoulder;
#   3. the Key Stone's emblem on the upper chest: a dark green ring round a glowing disc in a star of green crystal;
#   4. orange gems, cut and glowing, along the tail; the seeds down the back glow as gems.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, lathe, ring_band, lumpy, unit, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
BARK = "#2C452E"                           # the tree's trunk and branches
LEAF, LEAF2, LEAFLIT = "#3CC060", "#62DE80", "#8EF29C"   # the crown's glowing leaves, two greens, brighter points
GEM = "#FF9A62"                            # the tail's orange gems
STAR, RING, CORE = "#A8FFDC", "#1E5A38", "#58F096"   # the emblem's crystal star, its ring, its disc

# From sceptilemega.py's mega_tail(k, (0.0, 0.07, 0.44), (0.42, 0.85, -0.25), 0.78): the tail's root, way and length
TROOT = np.array([0.0, 0.07, 0.44])
TD = unit(0.42, 0.85, -0.25)
TLEN = 0.78


def join(*shapes):
    """Several closed shapes as one piece: a glowing piece is a material of its own, so like pieces go as one."""
    Vs, Fs, n = [], [], 0
    for V, F in shapes:
        Vs.append(np.asarray(V)); Fs += [[int(i) + n for i in f] for f in F]; n += len(V)
    return np.vstack(Vs), Fs


def gem(c, d, r, length):
    """A cut gem: a ring of eight corners round c across d, a crown to a table and a point below, flat faced."""
    a = np.cross(d, (0, 0, 1.0)); a = a / np.linalg.norm(a) if np.linalg.norm(a) > 1e-3 else np.array([1.0, 0, 0])
    b = np.cross(d, a)
    pts = []
    for i in range(8):
        t = 2 * math.pi * i / 8
        pts.append(c + r * (math.cos(t) * a + math.sin(t) * b))
        pts.append(c + d * length * 0.32 + r * 0.6 * (math.cos(t + 0.39) * a + math.sin(t + 0.39) * b))
    pts.append(c - d * length * 0.55)
    return hull(pts)


def clouds(k, cx, cy, rx, ry, n, size, seed, skip=()):
    """The clouds of Max energy on the ground: n clumps of round puffs of mixed sizes in a ring about (cx, cy), each in
    one colour with one puff of another, fused on the root bone (islands of their own), a few cyan curls riding on them."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    curls = []
    for i in range(n):
        if i in skip:
            continue
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
    if curls:
        k.add("curls", join(*curls), CYAN, "root", fuse=False, glow=CYAN)


def puffs(name, centre, radius, n, size, gap, bone, k, seed, lift=0.0):
    """A broken ring of crimson puffs floating level about centre (over the head), loose: a gap left at the front."""
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


def emblem(k, c, n, bone):
    """The Key Stone's emblem: a star of eight crystal points standing a hair off the chest, a dark ring and a glowing
    disc in it."""
    n = unit(*n)
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    outline = []
    for i in range(16):
        t = 2 * math.pi * i / 16 + math.pi / 16
        r = 0.066 if i % 2 == 0 else 0.034
        outline.append((r * math.cos(t), r * math.sin(t)))
    k.add("emblemstar", slab(outline, c + n * 0.006, ex, ez, 0.012), STAR, bone, fuse=False, glow=STAR)
    k.add("emblemring", ring_band(c + n * 0.013, n, 0.03, 0.01, thick=0.012), RING, bone, fuse=False)
    k.add("emblemcore", lathe(c + n * 0.009, n, [(0, 0), (0.026, 0), (0.026, 0.01), (0, 0.012)]), CORE, bone,
          fuse=False, glow=CORE)


def tree(k):
    """The tree grown from the tail's end: a fused dark trunk rising from the tail, branches out to a crown of
    glowing leaf clumps with brighter points among them, all on the tail's bone so it goes where the tail goes."""
    base = TROOT + TD * TLEN * 0.86
    top = np.array([base[0] - 0.02, base[1] - 0.02, 0.5])
    k.add("treetrunk", tube([base + (0, 0, -0.02), base + (0.0, 0.0, 0.12), top], [0.04, 0.035, 0.03]), BARK, "tail")
    C = np.array([top[0] - 0.06, top[1] - 0.01, 0.7])      # the crown's middle: a broad dome, wider than tall
    places = []
    for i in range(6):                                      # an outer ring, longer along y than across x
        a = 2 * math.pi * (i + 0.25) / 6
        places.append(C + (0.15 * math.cos(a), 0.2 * math.sin(a), -0.03 + 0.03 * (i % 2)))
    for i in range(4):                                      # an inner ring above it
        a = 2 * math.pi * (i + 0.6) / 4
        places.append(C + (0.085 * math.cos(a), 0.11 * math.sin(a), 0.09))
    places += [C + (0.0, 0.0, 0.15), C + (0.0, 0.0, -0.02)]
    # branches from the trunk's top out into the outer ring's clumps, dark among the leaves
    for i, e in enumerate(places[:6]):
        mid = top + (e - top) * 0.5 + (0, 0, 0.05)
        k.add(f"treebranch{i}", tube([top - (0, 0, 0.03), mid, top + (e - top) * 0.85], [0.022, 0.018, 0.016]),
              BARK, "tail")
    k.add("treebranch6", tube([top - (0, 0, 0.03), top + (0, 0, 0.16)], [0.024, 0.018]), BARK, "tail")
    clumps = {LEAF: [], LEAF2: []}
    points = []
    rnd = np.random.default_rng(31)
    for i, p in enumerate(places):
        r = (0.085, 0.08, 0.085, 0.08, 0.085, 0.08, 0.08, 0.075, 0.08, 0.075, 0.08, 0.08)[i]
        clumps[(LEAF, LEAF2)[i % 2]].append(ell(p, (r, r, r * 0.8), rot=(0, 0, 25 * i), deform=lumpy(3, 2, 0.25), sub=3))
        # leaf points standing out of each clump: thick flat-faced kites, outward and up
        out = p - C; out[2] = 0; out = out / (np.linalg.norm(out) + 1e-9)
        for j in range(3):
            th = rnd.uniform(-1.3, 1.3)
            ca, sa = math.cos(th), math.sin(th)
            dd = np.array([out[0] * ca - out[1] * sa, out[0] * sa + out[1] * ca, 0]) if np.linalg.norm(out) > 0.5 \
                else np.array([math.cos(3 * th), math.sin(3 * th), 0])
            d = unit(*(dd + np.array([0, 0, rnd.uniform(-0.1, 1.0)])))
            root = p + d * r * 0.6
            tip = p + d * (r + 0.065)
            side = np.cross(d, (0, 0, 1.0)); side = side / (np.linalg.norm(side) + 1e-9)
            nrm = np.cross(d, side)
            points.append(hull([root + side * 0.026, root - side * 0.026, root + nrm * 0.016, root - nrm * 0.016, tip]))
    for col, sh in clumps.items():
        k.add(f"treecrown{col[1:]}", join(*sh), col, "tail", fuse=False, glow=col, surface="leaf")
    k.add("treepoints", join(*points), LEAFLIT, "tail", fuse=False, glow=LEAFLIT, surface="leaf")


def maxed(k):
    """What the Mega Dynamax form lays over Mega Sceptile: called with the Mega's kit just before it is meshed."""
    tree(k)
    # The orange gems along the tail's top and its +X flank
    gems = []
    for t, side, r in ((0.3, 0.6, 0.036), (0.44, -0.2, 0.04), (0.57, 0.6, 0.036), (0.68, -0.3, 0.032)):
        up = np.array([0, 0, 1.0]); up = up - TD * (up @ TD); up /= np.linalg.norm(up)
        sd = np.cross(TD, up)
        rad = 0.06 - 0.03 * t
        n = unit(*(up + sd * side))
        c = TROOT + TD * TLEN * t + up * 0.03 * math.sin(math.pi * t) + n * (rad + 0.004)
        gems.append(gem(c, n, r, r * 1.6))
    k.add("tailgems", join(*gems), GEM, "tail", fuse=False, glow=GEM, surface="shell")
    # The seeds down the back glow as gems (the Mega's own parts, their colour kept)
    for p in k.parts:
        if p["name"].startswith(("seed", "oseed", "redseed")):
            p["glow"] = "#%02X%02X%02X" % tuple(int(round(255 * (c ** (1 / 2.2)))) for c in p["color"])
    # The emblem on the upper chest, where the torso's front is (the Mega's torso part)
    tor = next(p for p in k.parts if p["name"] == "torso")
    V = tor["V"]
    sel = V[(np.abs(V[:, 0]) < 0.015) & (np.abs(V[:, 2] - 0.645) < 0.02)]
    c = np.array([0.0, sel[:, 1].min(), 0.645])
    emblem(k, c, (0.12, -1, 0.15), "body")
    # The clouds: mounds about the feet (the feet stand at x 0.13, the toes reach y -0.17), clear of them all round
    clouds(k, 0.0, 0.0, 0.36, 0.3, 10, 0.075, 7)
    # A broken ring of crimson puffs floating just over the head, its gap at the front; a wisp over each shoulder
    puffs("headring", (0.0, 0.0, 1.005), 0.19, 19, 0.032, 60, "head", k, 5, lift=0.012)
    wisp("wispR", [(0.15, 0.06, 0.72), (0.2, 0.08, 0.77), (0.23, 0.06, 0.83)], (0.034, 0.028, 0.022), k, "body")
    wisp("wispL", [(-0.15, 0.06, 0.73), (-0.19, 0.09, 0.79)], (0.03, 0.024), k, "body", VIOLET)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("sceptilemega")
m.sceptilemega(*args())
