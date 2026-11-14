# Shaymin (Land Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shaymin.py -- <the export's Shaymin.glb> <out.glb>
#
# The design, from the references: a small white hedgehog, a round body low on four very short legs; a pointed white
# snout with a small black nose and a small open mouth; large green eyes; over its whole back and the top of its head
# a great mound of grass in shaggy tufts that sweep back, light yellow-green, with a few small yellow ovals in it; on
# each side of the head, where an ear would be, a large pink flower of five pointed petals with a darker pink middle
# and dark seeds, and behind each flower two broad dark green leaves.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, lathe, lumpy, srgb, inside, smoothstep, unit, eye_look, args, outward, \
    leaf, axes, polygon

src, out = args()
k = Kit(src, coat="fur")

WHITE, SHADE, NOSE = "#F2F4F8", "#DCE0EA", "#22222A"
GRASS, GRASS2, GRASS3 = "#8CCF58", "#A6DD6A", "#7CC24E"
LEAFG, LEAFV, SPECK = "#2E9A44", "#22803A", "#F2D23C"
PINK, PINKMID, SEED = "#F4A2B6", "#E06C8C", "#4A2830"
EYEG, EYEDARK, INK, MOUTH, TONGUE = "#38B04C", "#123A1E", "#1A2A1E", "#9A2C44", "#EE8494"
rs = np.random.RandomState(492)

B = np.array([0.0, 0.02, 0.108])          # the body's middle
BR = np.array([0.118, 0.135, 0.08])
H = np.array([0.0, -0.095, 0.155])        # the head's middle
HR = np.array([0.074, 0.07, 0.066])
M = np.array([0.0, 0.035, 0.2])          # the grass mound's middle
MR = np.array([0.145, 0.165, 0.11])


def blob(c, d, r):
    """An ellipsoid along a direction d: r = (across, thickness, along)."""
    a, b = axes(d)
    d = unit(*d)
    V, F = ell((0, 0, 0), r)
    return V @ np.stack([a, b, d]) + np.array(c, float), F


def on_head(d):
    d = unit(*d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


# --- the white hedgehog ---------------------------------------------------------------------------------------------
k.add("body", ell(B, BR), WHITE, "body")
k.add("head", ell(H, HR), WHITE, "head")
def snout_taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * t; V[:, 2] *= 1 - 0.5 * t
    return V
k.add("snout", ell((0, -0.15, 0.137), (0.042, 0.055, 0.036), deform=snout_taper), WHITE, "head")
k.add("nose", ell((0, -0.2045, 0.142), (0.011, 0.008, 0.009)), NOSE, "head", fuse=False, surface="skin")
# Four very short legs
for nm, bone, y in (("leg", "leg{s}", -0.06), ("hind", "hind{s}", 0.075)):
    k.add(nm, tube([(0.062, y, 0.075), (0.064, y - 0.004, 0.04), (0.064, y - 0.008, 0.03)],
                   [0.034, 0.031, 0.028]), WHITE, bone, mirror=True)

# --- the mound of grass ---------------------------------------------------------------------------------------------
k.add("grass", ell(M, MR, deform=lumpy(5, 4, 0.08)), GRASS, "body", surface="leaf")
# Shaggy tufts sweeping back over it, two greens, each laid along the mound with its tip out and back
n = 0
TOPS = []
for row, (el, count) in enumerate(((1.25, 1), (0.9, 6), (0.55, 9), (0.2, 11), (-0.15, 11), (-0.45, 12))):
    for j in range(count):
        az = 2 * math.pi * (j + 0.5 * (row % 2)) / count + rs.uniform(-0.15, 0.15)
        o = np.array([math.cos(el) * math.sin(az), -math.cos(el) * math.cos(az), math.sin(el)])
        p = M + o * MR * 0.92
        if p[1] < -0.06 and p[2] < 0.24:
            continue                            # the face stays clear under the mound's front edge
        d = o * 0.55 + np.array([0, 1.0, 0.12])
        d[0] += o[0] * 0.3
        L = rs.uniform(0.05, 0.065)
        cen = p + unit(*d) * L * 0.4
        if o[2] > 0.3:
            TOPS.append(cen + axes(d)[1] * 0.017)
        k.add(f"tuft{n}", blob(cen, d, (rs.uniform(0.03, 0.038), 0.02, L)),
              (GRASS, GRASS2, GRASS3)[rs.randint(3)], "body", surface="leaf")
        n += 1
# The skirt: tufts hanging down and back over the flanks and the rump, on the body's own outline, so that the grass
# comes down to about a third of the body's height at the back
for row, (z, count, Lh) in enumerate(((0.168, 14, 0.06), (0.128, 16, 0.065))):
    for j in range(count):
        az = 2 * math.pi * (j + 0.5 * row) / count + rs.uniform(-0.1, 0.1)
        o = np.array([math.sin(az), -math.cos(az), 0.0])
        if o[1] < 0.1:
            continue                            # the flanks under the flowers stay white, as in the pictures
        f = math.sqrt(max(0.0, 1 - ((z - B[2]) / BR[2]) ** 2))
        p = np.array([B[0] + o[0] * BR[0] * f, B[1] + o[1] * BR[1] * f, z]) + o * 0.012
        d = o * 0.45 + np.array([0, 0.35, -1.0])
        k.add(f"skirt{n}", blob(p + unit(*d) * Lh * 0.35, d, (rs.uniform(0.032, 0.04), 0.02, Lh)),
              (GRASS, GRASS3)[rs.randint(2)], "body", surface="leaf")
        n += 1

# The brim: tufts hanging forward over the brow
for x in (-0.06, 0.0, 0.06):
    k.add(f"brim{n}", blob((x, -0.105, 0.232), (x * 2, -0.6, 0.55), (0.032, 0.02, 0.045)), GRASS2, "body",
          surface="leaf")
    n += 1

# Small yellow ovals in the grass, painted on the tops of a few tufts
SPOTS = [TOPS[i] for i in rs.choice(len(TOPS), 6, replace=False)]
def specks(P):
    m = np.zeros(len(P))
    for t in SPOTS:
        m = np.maximum(m, inside(np.linalg.norm((P - t) / (0.028, 0.02, 0.028), axis=1) - 1, 0.08))
    return m
k.paint(specks, SPECK, on=["tuft"])

# --- the face -------------------------------------------------------------------------------------------------------
CM, CT = srgb(MOUTH), srgb(TONGUE)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.6) ** 2 + ((v + 0.75) / 0.5) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.35 / s) if s > 0 else 1.0
dm = unit(0, -0.8, -0.6)
k.decal((0, -0.165, 0.112), dm, (0.024, 0.019), mouth, eps=0.003, outline=mouth_shape, rings=12, segs=48)


# --- the flowers, one each side where an ear would be, turned some 40 degrees to the front -------------------------
def frame(c, n_):
    """Two directions in the flower's plane, the first as near up as it can be."""
    n_ = unit(*n_)
    up = np.array([0, 0, 1.0]) - n_ * n_[2]
    up /= np.linalg.norm(up)
    return n_, up, np.cross(n_, up)

L = 0.094
PETAL = [(0.0, -0.012), (0.3 * L, -0.33 * L), (0.62 * L, -0.34 * L), (0.82 * L, -0.2 * L), (L, 0.0),
         (0.82 * L, 0.2 * L), (0.62 * L, 0.34 * L), (0.3 * L, 0.33 * L), (0.0, 0.012)]
for s, side in ((1, "R"), (-1, "L")):
    c = np.array([0.138 * s, -0.07, 0.19])
    nrm, p_, q_ = frame(c, (0.72 * s, -0.56, 0.4))
    if s < 0:
        q_ = -q_
    for i in range(5):
        th = math.pi / 2 + 2 * math.pi * i / 5
        ex = math.cos(th) * p_ + math.sin(th) * q_
        ex = unit(*(ex + nrm * 0.22))            # cupped a little toward the viewer
        ez = math.cos(th + math.pi / 2) * p_ + math.sin(th + math.pi / 2) * q_
        k.add(f"petal{side}{i}", slab(PETAL, c + nrm * 0.0015 * i, ex, ez, 0.009), PINK, "head", fuse=False,
              surface="leaf")
        # A dark seed on each petal near the middle
        sd = c + nrm * 0.012 + unit(*(math.cos(th) * p_ + math.sin(th) * q_)) * 0.026
        a_ = unit(*(math.cos(th) * p_ + math.sin(th) * q_))
        V, F = ell((0, 0, 0), (0.0045, 0.003, 0.009))
        k.add(f"seed{side}{i}", outward((V @ np.stack([np.cross(nrm, a_), nrm, a_]) + sd, F)), SEED, "head",
              fuse=False, surface="leaf")
    k.add(f"heart{side}", lathe(c + nrm * 0.006, nrm, [(0, -0.004), (0.016, -0.004), (0.017, 0.002), (0.012, 0.006),
                                                       (0, 0.007)]), PINKMID, "head", fuse=False, surface="leaf")
    # Two broad dark green leaves lying back along the flank behind the flower, one up and one down, their faces
    # out and a little up and back so that the back view sees them; a darker vein down each
    t = np.linspace(0, 1, 15)
    half = 0.04 * np.sin(math.pi * t) ** 0.8
    LEAF = [(0.17 * a, b) for a, b in zip(t, half)] + [(0.17 * a, -b) for a, b in zip(t[::-1][1:-1], half[::-1][1:-1])]
    VEIN = [(0.01, -0.004), (0.13, -0.002), (0.13, 0.002), (0.01, 0.004)]
    for j, d in enumerate(((0.3 * s, 1.0, 0.32), (0.42 * s, 1.0, -0.42))):
        d = unit(*d)
        nl = np.array([0.82 * s, 0.32, 0.45]); nl = nl - d * (nl @ d); nl /= np.linalg.norm(nl)
        ez = np.cross(nl, d)
        base = c - nrm * 0.01 + np.array([0.0, 0.025, 0.0])
        k.add(f"leaf{side}{j}", slab(LEAF, base, d, ez, 0.014), LEAFG, "body", fuse=False, surface="leaf")
        k.add(f"vein{side}{j}", slab(VEIN, base + nl * 0.008, d, ez, 0.004), LEAFV, "body", fuse=False, surface="leaf")

k.build(voxel=0.0045, band=0.004)

# --- large green eyes, forward on the face ----------------------------------------------------------------------
CW, CI, CG, CD = srgb(WHITE), srgb(INK), srgb(EYEG), srgb(EYEDARK)
def eye(u, v):
    c = np.tile(CG, (len(u), 1))
    c[(u / 0.34) ** 2 + ((v + 0.05) / 0.62) ** 2 < 1] = CD
    c[((u + 0.2) / 0.22) ** 2 + ((v - 0.38) / 0.2) ** 2 < 1] = (1, 1, 1)
    c[np.sqrt(u * u + v * v) > 0.82] = CI
    return c
EP, EN = on_head((0.6, -0.68, 0.32))
def eyes(state):
    k.decal(EP, EN, (0.021, 0.026), eye_look(state, eye, CW, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
