# Mega Dynamax Swampert (a form of this project's own) as Mega Swampert's own sculpt (swampertmega.py), whole and
# unchanged, with what the form lays over it put on the Mega's kit just before it is meshed (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swampertmegammax.py -- <the export's Swampert-Mega-Mmax.glb> <out.glb>
# What the picture (docs/art/reference/gemini/megadynamax/Mega-Dynamax-Swampert.jpg) adds to the Mega, largest first:
#   1. a great breaking wave of crystal water rising from behind the hump, arching up and curling forward over it,
#      ribs of faceted aqua, deep blue and pale glass side by side, spray at its crest;
#   2. the two long crests from the brow turned to aqua crystal (the Mega's own crests, their colour changed and lit);
#   3. the clouds of Max energy: fused mounds about the fists and feet, crimson and violet by turns with cyan curls,
#      a broken ring of crimson puffs over the hump and a wisp over each shoulder;
#   4. the Key Stone's emblem on the pale chest under the jaw: a glowing disc with a four-pointed star in a bright ring.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, slab, sheet, lathe, ring_band, srgb, unit, outward, sibling, args

CRIMSON, DEEP, VIOLET, DVIOLET, CYAN = "#AC223E", "#701630", "#6C3EA0", "#42246E", "#4EE2EC"
AQUA, BLUEW, GLASS, DEEPW = "#6EE6EC", "#46AADC", "#B4F2F8", "#2E7CC4"   # the wave's crystal
CRESTGLOW = "#7EE8F0"                                        # the crests turned to crystal
EM_DISC, EM_RING, EM_STAR = "#FFF4D0", "#B48CFF", "#FF8CC8"   # the emblem, prismatic as the picture's


def join(*shapes):
    """Several closed shapes as one piece: a glowing piece is a material of its own, so like pieces go as one."""
    Vs, Fs, n = [], [], 0
    for V, F in shapes:
        Vs.append(np.asarray(V)); Fs += [[int(i) + n for i in f] for f in F]; n += len(V)
    return np.vstack(Vs), Fs


def segment(a, b, w0, w1, t0, t1, across=(1.0, 0, 0)):
    """A faceted piece of the wave from a to b: a flat-faced block w wide across x and t thick across its own way."""
    a, b = np.array(a, float), np.array(b, float)
    d = unit(*(b - a))
    ex = np.array(across, float); ex = ex - d * (ex @ d); ex /= np.linalg.norm(ex)
    nn = np.cross(d, ex)
    return hull([a + ex * w0 + nn * t0, a - ex * w0 + nn * t0, a + ex * w0 - nn * t0, a - ex * w0 - nn * t0,
                 b + ex * w1 + nn * t1, b - ex * w1 + nn * t1, b + ex * w1 - nn * t1, b - ex * w1 - nn * t1])


X0, X1 = -0.27, 0.33                     # the wave's breadth across the back


def chaikin(P, rounds=4):
    """A polyline rounded by corner cutting, its ends kept."""
    P = np.array(P, float)
    for _ in range(rounds):
        Q = [P[0]]
        for a, b in zip(P[:-1], P[1:]):
            Q += [0.75 * a + 0.25 * b, 0.25 * a + 0.75 * b]
        Q.append(P[-1])
        P = np.array(Q)
    return P


def height(x):
    return max(0.3, 1 - ((x - 0.1) / 0.36) ** 2)        # tallest a little to +X


def arc(x, nv, jig=0.0):
    """The wave's section at x (y, z along it from the back's root, up behind, over the crest and curling forward over
    the hump's back, short of the head); jig lifts or lowers a strip a little."""
    h = height(x) * (1 + jig)
    top = 0.6 + 0.32 * h
    ctrl = [(0.06, 0.5), (0.24, 0.56), (0.33, top - 0.14), (0.27, top - 0.01), (0.13, top + 0.01),
            (0.04, top - 0.05), (0.0, top - 0.12)]
    C = chaikin(ctrl)
    s = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(C, axis=0), axis=1))])
    t = np.linspace(0, s[-1], nv)
    return np.stack([np.interp(t, s, C[:, 0]), np.interp(t, s, C[:, 1])], axis=1)


def wave_sheet(v0, v1, xa, xb, out, nu=8, nv=44, thick=0.03, jig=0.0):
    """The wave's surface between v0 and v1 along its section and from xa to xb across, lifted off the main surface
    by out (toward the outside of the curl, or inside it when negative), a sheet with thickness."""
    xs = np.linspace(xa, xb, nu)
    P = np.empty((nu, nv, 3))
    for i, x in enumerate(xs):
        A = arc(x, 200, jig)
        idx = np.linspace(v0, v1, nv) * 199
        yz = np.stack([np.interp(idx, np.arange(200), A[:, 0]), np.interp(idx, np.arange(200), A[:, 1])], axis=1)
        d = np.gradient(yz, axis=0); d /= np.linalg.norm(d, axis=1, keepdims=True) + 1e-12
        nrm = np.stack([d[:, 1], -d[:, 0]], axis=1)      # to the outside of the curl
        yz = yz + nrm * out
        P[i, :, 0] = x * (1 + 0.1 * np.linspace(v0, v1, nv))
        P[i, :, 1:] = yz
    return outward(sheet(lambda u, v: P, nu, nv, thick))


def wave(k):
    """The wave: strips of crystal water side by side, each a smooth sheet from the back up behind, over the crest and
    curling forward over the hump, of staggered heights and ends, aqua, blue and pale by turns, each ending in a point
    of its own colour; its inner face deep blue; jagged points of glass standing off its back."""
    groups = {AQUA: [], BLUEW: [], GLASS: []}
    cols = (AQUA, BLUEW, AQUA, GLASS)
    points = {AQUA: [], BLUEW: [], GLASS: []}
    spikes = []
    rnd = np.random.default_rng(2)
    edges = np.linspace(X0, X1, 11)
    for i, (xa, xb) in enumerate(zip(edges[:-1], edges[1:])):
        jig = rnd.uniform(-0.05, 0.05)
        v1 = rnd.uniform(0.86, 1.0)
        col = cols[i % 4]
        groups[col].append(wave_sheet(0.0, v1, xa - 0.004, xb + 0.004, 0.004 * (i % 2), thick=0.034, jig=jig))
        # the strip's point off its end, forward and down, in its own colour
        x = (xa + xb) / 2
        A = arc(x, 200, jig)
        e, f = A[int(v1 * 199)], A[int(v1 * 199) - 6]
        d = unit(0, e[0] - f[0], e[1] - f[1])
        sx = x * (1 + 0.1 * v1)
        root = np.array([sx, e[0], e[1]]) - d * 0.03
        ex = np.array([1.0, 0, 0]); nn = np.cross(d, ex)
        w = (xb - xa) / 2 + 0.002
        points[col].append(hull([root + ex * w + nn * 0.017, root - ex * w + nn * 0.017, root + ex * w - nn * 0.017,
                                 root - ex * w - nn * 0.017, root + d * (0.06 + 0.03 * rnd.uniform())]))
    for col in groups:
        k.add(f"wave{col[1:]}", join(*(groups[col] + points[col])), col, "body", fuse=False, glow=col)
    k.add("waveinner", wave_sheet(0.33, 0.84, X0 + 0.02, X1 - 0.02, -0.032, nu=30, thick=0.016), DEEPW, "body",
          fuse=False, glow=DEEPW)
    for i, x in enumerate(np.linspace(X0 + 0.03, X1 - 0.03, 11)):
        A = arc(x, 60)
        sx = x * 1.1
        ex = np.array([1.0, 0, 0])
        # spikes standing back and up off the wave's outer back
        if i % 2 == 0:
            j = 24 + (i % 4) * 2
            p, q = A[j], A[j + 2]
            dd = unit(0, q[0] - p[0], q[1] - p[1])
            out = np.cross(dd, ex)
            out = out if out[1] > 0 else -out
            root = np.array([sx, p[0], p[1]])
            dir_ = unit(*(out * 0.8 + dd * 0.6))
            nn2 = np.cross(dir_, ex)
            spikes.append(hull([root + ex * 0.022, root - ex * 0.022, root + nn2 * 0.014, root - nn2 * 0.014,
                                root + dir_ * 0.08]))
    k.add("wavespikes", join(*spikes), GLASS, "body", fuse=False, glow=GLASS)


def clouds(k, cx, cy, rx, ry, n, size, seed, keep=()):
    """The clouds of Max energy on the ground: n clumps of round puffs in a ring about (cx, cy), fused on the root bone
    (islands of their own), each in one colour with one puff of another, a few cyan curls riding on them. keep:
    (x, y, r) circles a clump is pushed out of (a fist, a foot), so no web of the remesh joins it to the body."""
    rnd = np.random.default_rng(seed)
    cols = (CRIMSON, VIOLET, DEEP, DVIOLET)
    curls = []
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        col = cols[i % 4]
        c0 = np.array([cx + rx * math.cos(a), cy + ry * math.sin(a)])
        c1 = c0.copy()
        for kx, ky, kr in keep:
            d = c1 - (kx, ky)
            if np.linalg.norm(d) < kr:
                c1 = np.array([kx, ky]) + d / np.linalg.norm(d) * kr
        shift = np.array([*(c1 - c0), 0.0])
        for j in range(4):
            s = size * (1.0, 0.78, 0.66, 0.5)[j] * (1 + 0.12 * rnd.uniform(-1, 1))
            t = a + (0.0, 0.16, -0.15, 0.03)[j] * (0.33 / max(rx, ry))
            g = (0.0, 0.03, 0.025, -0.01)[j]
            p = np.array([cx + (rx + g) * math.cos(t), cy + (ry + g) * math.sin(t), s * (0.93, 0.95, 0.93, 2.1)[j]])
            k.add(f"cloud{i}_{j}", ell(p + shift, (s, s, s * 0.9)), col if j != 2 else cols[(i + 1) % 4], "root")
        if i % 3 == 0:
            q = np.array([cx + (rx + 0.03) * math.cos(a + 0.05), cy + (ry + 0.03) * math.sin(a + 0.05), size * 1.55])
            curls.append(ell(q + shift, (size * 0.32, size * 0.32, size * 0.28), sub=3))
    k.add("curls", join(*curls), CYAN, "root", fuse=False, glow=CYAN)


def wisp(name, pts, sizes, k, bone, colour=CRIMSON):
    """A curl of cloud: a few loose puffs along a short path."""
    k.add(name, join(*[ell(p, (s, s, s * 0.9), sub=3) for p, s in zip(pts, sizes)]), colour, bone, fuse=False)


def maxed(k):
    """What the Mega Dynamax form lays over Mega Swampert: called with the Mega's kit just before it is meshed."""
    wave(k)
    # The crests turned to crystal: the Mega's own crest parts, their colour changed and lit
    for p in k.parts:
        if p["name"].startswith("crest"):
            p["color"] = srgb(CRESTGLOW)
            p["glow"] = CRESTGLOW
    # The emblem: the picture has it on the pale chest under the jaw, where the Mega's low head hides it from both of
    # the game's cameras (built there it showed as a sliver between the fists), so it is on the brow between the two
    # crests, over the eyes, facing forward and up (the Mega's head part)
    V = next(p for p in k.parts if p["name"] == "head")["V"]
    yc = -0.36
    sel = V[(np.abs(V[:, 0]) < 0.012) & (np.abs(V[:, 1] - yc) < 0.012)]
    n = unit(0.0, -0.5, 0.87)
    c = np.array([0.0, yc, sel[:, 2].max() + 0.003])
    k.add("emblemdisc", lathe(c, n, [(0, 0), (0.024, 0), (0.024, 0.007), (0, 0.009)]), EM_DISC, "head", fuse=False,
          glow=EM_DISC)
    k.add("emblemring", ring_band(c + n * 0.004, n, 0.03, 0.01, thick=0.01), EM_RING, "head", fuse=False,
          glow=EM_RING)
    ex = np.cross((0, 0, 1.0), n); ex /= np.linalg.norm(ex); ez = np.cross(n, ex)
    star = [(0.02, 0), (0.005, 0.005), (0, 0.02), (-0.005, 0.005), (-0.02, 0), (-0.005, -0.005), (0, -0.02),
            (0.005, -0.005)]
    k.add("emblemstar", slab(star, c + n * 0.01, ex, ez, 0.005), EM_STAR, "head", fuse=False, glow=EM_STAR)
    # The clouds: mounds about the fists (x 0.28, y -0.46) and the hind feet (x 0.3, y 0.25), clear of them all round
    keep = [(s * 0.185, -0.39, 0.205) for s in (1, -1)] + [(s * 0.225, 0.15, 0.175) for s in (1, -1)]
    clouds(k, 0.0, -0.07, 0.33, 0.42, 12, 0.056, 13, keep)
    # The picture has no ring over the head (the wave stands there): a curl of crimson cloud over each shoulder
    wisp("wispR", [(0.27, -0.06, 0.55), (0.31, -0.03, 0.6), (0.33, -0.07, 0.65), (0.29, -0.1, 0.62)],
         (0.042, 0.036, 0.028, 0.03), k, "body")
    wisp("wispL", [(-0.27, -0.04, 0.56), (-0.31, -0.01, 0.62), (-0.33, -0.05, 0.66)], (0.04, 0.032, 0.026), k, "body",
         DEEP)


_build, _laid = Kit.build, []
def build(self, *a, **kw):
    if not _laid:               # the first build is the body's; the kit builds the eyes' five states after it
        _laid.append(True)
        maxed(self)
    return _build(self, *a, **kw)
Kit.build = build

m = sibling("swampertmega")
m.swampertmega(*args())
