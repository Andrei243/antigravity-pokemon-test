# Shaymin (Sky Forme) sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shayminsky.py -- <the export's Shaymin-Sky.glb> <out.glb>
#
# Grown from shaymin.py (the Land Forme): its whites, grass greens, pink-red and its eye. The design, from the
# references: a small slender white deer standing tall on four thin legs; a white body with a fluffy chest ruff and
# small tufts at the shoulders; the lower legs green like boots with pointed cuffs; a small head with a short pointed
# snout, a black nose, a smile and a green eye with a dark lid line; a green crest of grass swept back over the head
# and down the nape; for ears two large white wings of four pointed feathers raised in a V; a red flower of two long
# pointed petals with dark seeds on the right side of the neck, trailing back; a small tufted tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, hull, srgb, inside, unit, eye_look, args, outward, axes, tri

src, out = args()
k = Kit(src, coat="fur")

WHITE, SHADE, NOSE = "#F2F4F8", "#DCE0EA", "#22222A"
GRASS, GRASS2, GRASS3 = "#8CCF58", "#A6DD6A", "#7CC24E"
RED, REDDARK, SEED = "#DC1E3C", "#B4142E", "#4A2830"
EYEG, EYEDARK, INK = "#38B04C", "#123A1E", "#1A2A1E"

B = np.array([0.0, 0.008, 0.268])          # the body's middle: hung low over the legs' tops
BR = np.array([0.084, 0.128, 0.088])
H0 = np.array([0.0, -0.118, 0.456])        # where the head's parts were first laid out (they are scaled about H)
H = np.array([0.0, -0.13, 0.468])          # the head's middle
HS = 1.2                                   # the head, snout, eyes and crest a fifth larger than first laid out
HR = np.array([0.077, 0.073, 0.071]) * HS


def at_head(p):
    """A point laid out round the first head, moved onto the larger one."""
    return H + (np.array(p, float) - H0) * HS
LEGS = (("leg", "leg{s}", -0.072), ("hind", "hind{s}", 0.085))
LX = 0.043


def blob(c, d, r):
    """An ellipsoid along a direction d: r = (across, thickness, along)."""
    a, b = axes(d)
    d = unit(*d)
    V, F = ell((0, 0, 0), r)
    return V @ np.stack([a, b, d]) + np.array(c, float), F


def pointed(c, d, r):
    """A tuft: an ellipsoid along d that tapers to a point at its far end."""
    a, b = axes(d)
    d = unit(*d)
    def taper(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        V[:, 0] *= 1 - 0.85 * t ** 1.5; V[:, 1] *= 1 - 0.85 * t ** 1.5
        return V
    V, F = ell((0, 0, 0), r, deform=taper)
    return V @ np.stack([a, b, d]) + np.array(c, float), F


def on_head(d):
    d = unit(*d)
    p = H + d / math.sqrt(((d / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


# --- the white body ---------------------------------------------------------------------------------------------------
k.add("body", ell(B, BR, rot=(-10, 0, 0)), WHITE, "body")
k.add("chest", ell((0, -0.092, 0.315), (0.072, 0.066, 0.08)), WHITE, "body")
k.add("neck", tube([(0, -0.075, 0.33), (0, -0.1, 0.38), (0, -0.12, 0.425)], [0.055, 0.05, 0.05]), WHITE, "head")
k.add("head", ell(H, HR), WHITE, "head")
def snout_taper(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * t; V[:, 2] *= 1 - 0.5 * t
    return V
k.add("snout", ell(at_head((0, -0.175, 0.432)), np.array([0.035, 0.05, 0.032]) * HS, deform=snout_taper), WHITE,
      "head")
k.add("nose", ell(at_head((0, -0.2245, 0.437)), np.array([0.01, 0.008, 0.009]) * HS), NOSE, "head", fuse=False,
      surface="skin")

# Shoulders and thighs: broad masses over the tops of the legs; then short springy legs, splayed (fore feet forward
# and apart, hind feet back and apart), tapering to the hoof, never sticks
k.add("shoulder", ell((LX + 0.004, -0.075, 0.225), (0.042, 0.05, 0.068)), WHITE, "leg{s}", mirror=True)
k.add("thigh", ell((LX + 0.006, 0.085, 0.225), (0.046, 0.06, 0.072)), WHITE, "hind{s}", mirror=True)
FORE = [(LX, -0.075, 0.22), (0.056, -0.098, 0.12), (0.069, -0.118, 0.04), (0.072, -0.128, 0.014)]
HIND = [(LX, 0.088, 0.22), (0.057, 0.108, 0.13), (0.069, 0.124, 0.04), (0.072, 0.13, 0.014)]
k.add("leg", tube(FORE, [0.036, 0.024, 0.018, 0.016]), WHITE, "leg{s}", mirror=True)
k.add("hind", tube(HIND, [0.04, 0.025, 0.018, 0.016]), WHITE, "hind{s}", mirror=True)

# The chest ruff: pointed tufts hanging down and forward under the chin, and a tuft at each shoulder pointing back
for i, (x, z, dx) in enumerate(((0.0, 0.3, 0.0), (0.028, 0.305, 0.4), (-0.028, 0.305, -0.4), (0.018, 0.33, 0.6),
                                (-0.018, 0.33, -0.6))):
    k.add(f"ruff{i}", pointed((x, -0.15, z), (dx, -0.5, -1.0), (0.022, 0.016, 0.045)), WHITE, "body")
k.add("elbow", pointed((0.065, -0.06, 0.25), (0.6, 1.0, 0.25), (0.018, 0.014, 0.04)), WHITE, "leg{s}", mirror=True)
k.add("hip", pointed((0.07, 0.07, 0.24), (0.5, 1.0, 0.1), (0.016, 0.013, 0.035)), WHITE, "hind{s}", mirror=True)

# A small tufted tail
for i, d in enumerate(((0, 1.0, 0.7), (0.5, 1.0, 0.4), (-0.5, 1.0, 0.4))):
    k.add(f"tail{i}", pointed((0, 0.13, 0.3), d, (0.022, 0.017, 0.05)), WHITE, "tail")

# --- the boots: green below a cuff of pointed tips on every leg ------------------------------------------------------
def boots(P):
    sx = np.where(P[:, 0] >= 0, 1.0, -1.0)
    s = np.clip((0.12 - P[:, 2]) / 0.08, 0, 1)            # the legs' line below their knees, splayed out
    ly = np.where(P[:, 1] < 0.0, -0.098 - 0.02 * s, 0.108 + 0.016 * s)
    ang = np.arctan2(P[:, 1] - ly, P[:, 0] - sx * (0.056 + 0.013 * s))
    top = 0.095 + 0.028 * (tri(3 * ang / (2 * math.pi) + 0.25) + 1) / 2
    return inside(P[:, 2] - top, 0.003) * inside(np.abs(P[:, 0]) - 0.11, 0.001)
k.paint(boots, GRASS3)

# --- the crest: green over the top of the head and down the nape, with spikes swept back -----------------------------
def cap(P):
    d = (P - H) / HR
    jag = 0.06 * tri(3.0 * np.arctan2(d[:, 0], d[:, 1] + 0.3) / math.pi)
    m = inside(-(0.75 * d[:, 2] + 0.62 * d[:, 1] - 0.12 + jag), 0.004)
    return m * inside(np.linalg.norm(d, axis=1) - 1.35, 0.01) * inside(0.37 - P[:, 2], 0.01)
k.paint(cap, GRASS, on=["head", "neck"])

SPIKES = (((0.0, -0.13, 0.505), (0.0, -0.4, 1.0), 0.07),
          ((0.0, -0.105, 0.508), (0.0, 0.7, 1.0), 0.1),
          ((0.024, -0.095, 0.5), (0.45, 0.8, 0.8), 0.08),
          ((-0.024, -0.095, 0.5), (-0.45, 0.8, 0.8), 0.08),
          ((0.0, -0.075, 0.49), (0.0, 1.0, 0.55), 0.09),
          ((0.0, -0.05, 0.46), (0.0, 1.0, 0.15), 0.07))
for i, (root, d, L) in enumerate(SPIKES):
    root = at_head(np.array(root) + (0, 0, 0.012)); d = unit(*(np.array(d) + (0, 0.25, 0)))   # swept further back
    L = L * HS * 1.15
    a, b = axes(d)
    w, t = 0.032 * HS, 0.017 * HS
    pts = [root + a * w, root - a * w, root + b * t, root - b * t, root - d * 0.02, root + d * L]
    k.add(f"spike{i}", hull(pts), (GRASS, GRASS2)[i % 2], "head", fuse=False, surface="leaf")

# --- the wings, where the ears are: four pointed feathers, raised in a V and turned to show their faces --------------
WING = [(0.0, -0.022), (0.07, -0.042), (0.16, -0.07), (0.13, -0.034), (0.21, -0.034), (0.165, -0.008),
        (0.215, 0.022), (0.16, 0.03), (0.185, 0.066), (0.115, 0.05), (0.05, 0.034), (0.0, 0.022)]
# Each as long as the body, reaching out from the side of the head and a little up (some 18 degrees), swept back
ROOT = at_head((0.05, -0.1, 0.482))
ex = unit(0.88, 0.36, 0.3)
ez = np.array([0.0, -0.3, 1.0]) - ex * (ex @ np.array([0.0, -0.3, 1.0]))
k.add("wing", slab([(a * 1.47, b * 1.4) for a, b in WING], ROOT, ex, ez, 0.02), WHITE, "ear{s}", fuse=False,
      mirror=True)

# --- the red flower on the right of the neck (+X), two long pointed petals trailing back ------------------------------
KNOT = np.array([0.045, -0.07, 0.37])
PETAL = [(0.0, 0.0), (0.03, -0.022), (0.08, -0.03), (0.13, -0.022), (0.17, 0.0), (0.12, 0.02), (0.07, 0.026),
         (0.03, 0.02)]
for j, d in enumerate(((0.35, 1.0, 0.32), (0.45, 1.0, -0.38))):
    d = unit(*d)
    nl = unit(0.85, -0.25, 0.4); nl = nl - d * (nl @ d); nl /= np.linalg.norm(nl)
    ez_ = np.cross(nl, d)
    k.add(f"petal{j}", slab(PETAL, KNOT + d * 0.005, d, ez_, 0.014), RED, "body", fuse=False, surface="leaf")
    # two dark seeds on each petal near the knot, on the face turned out
    for q, off in enumerate((-0.008, 0.008)):
        sp = KNOT + d * (0.035 + 0.01 * q) + ez_ * off + nl * 0.0075
        V, F = ell((0, 0, 0), (0.0035, 0.002, 0.008))
        k.add(f"seed{j}{q}", outward((V @ np.stack([ez_, nl, d]) + sp, F)), SEED, "body", fuse=False,
              surface="leaf")
k.add("knot", ell(KNOT, (0.01, 0.014, 0.014)), REDDARK, "body", fuse=False, surface="leaf")

# --- the face: a smile under the snout --------------------------------------------------------------------------------
CW, CI = srgb(WHITE), srgb(INK)
def smile(u, v):
    c = np.tile(CW, (len(u), 1))
    c[(np.abs(v - (-0.35 + 0.75 * u * u)) < 0.3) & (np.abs(u) < 0.9)] = CI
    return c
k.decal(at_head((0, -0.2, 0.416)), unit(0, -0.8, -0.6), (0.026 * HS, 0.011 * HS), smile, eps=0.003, rings=12, segs=48)

k.build(voxel=0.0048, band=0.004)

# --- green eyes with a dark lid line slanting down to the nose -------------------------------------------------------
CG, CD = srgb(EYEG), srgb(EYEDARK)
def eye(u, v):
    c = np.tile(CG, (len(u), 1))
    c[(u / 0.36) ** 2 + ((v + 0.1) / 0.55) ** 2 < 1] = CD
    c[((u + 0.15) / 0.2) ** 2 + ((v - 0.15) / 0.18) ** 2 < 1] = (1, 1, 1)
    c[np.sqrt(u * u + v * v) > 0.84] = CI
    lid = 0.48 + 0.18 * u
    c[v > lid] = CW
    c[(v > lid - 0.24) & (v <= lid)] = CI
    return c
EP, EN = on_head((0.58, -0.72, 0.28))
def eyes(state):
    k.decal(EP, EN, (0.021 * HS, 0.026 * HS), eye_look(state, eye, CW, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
