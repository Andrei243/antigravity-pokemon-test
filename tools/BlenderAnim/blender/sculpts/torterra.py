# Torterra sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/torterra.py -- <the export's Torterra.glb> <out.glb>
#
# The design, from the references: a massive brown body on four pillar legs, three grey claws on each foot; a green
# shell, its plates parted by dark seams, running back to a green pointed tail; a grey band of rock round the shell,
# high over the head and low at the back, with a rock spike at each of its corners; tall grey rock spikes on the shell;
# a tree growing from a brown patch of soil on the back, its trunk forking and a root lying over the shell, its crown a
# cloud of leafy lumps; a heavy green head under a plate of shell, a black hooked beak, brown under the jaw, and white
# eyes with red irises under a scowling brow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args, sheet, lumpy, facets

src, out = args()
k = Kit(src, coat="scales")

BROWN, LEGS, SHELL, SEAM, PLATE, ROCK, ROCKD, BEAK, TRUNK, SOIL, CLAW = \
    "#B98650", "#C59258", "#3F9C4B", "#2A6A34", "#358A41", "#B8BCC3", "#A2A7AE", "#33343A", "#8A5A33", "#A97A48", \
    "#E2E2E4"
LEAF, LEAF2, LEAF3 = "#3E9E4E", "#55B25E", "#2E7E3E"
E = 0.02                                                    # a paint's edge: about a voxel

def dome(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.35, V[:, 2])
    return V

# Body, neck and head
k.add("body", ell((0, 0.02, 0.44), (0.5, 0.64, 0.26)), BROWN, "body")
H = np.array([0, -0.86, 0.52])
k.add("neck", tube([(0, -0.4, 0.48), (0, -0.7, 0.5)], [0.25, 0.22]), BROWN, "head")
k.add("head", ell(H, (0.25, 0.27, 0.22), deform=boxy(2.3)), SHELL, "head")
def hook(V):
    V = boxy(3.2)(V)                                            # squared: a jaw with edges, not a nose
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.7 * t ** 1.1                               # a wide cap narrowing straight to the tip
    V[:, 2] = V[:, 2] * (1 - 0.45 * t) - 1.0 * t ** 2.2         # the tip hooks down
    V[:, 1] += 0.25 * t * np.clip(-V[:, 2], 0, 1)               # its underside swept back: a hook, not a ball
    return V
# Loose, so its edges stay angular (the remesh rounds a fused one into a nose)
k.add("beak", ell((0, -1.05, 0.555), (0.19, 0.18, 0.1), rot=(-6, 0, 0), deform=hook), BEAK, "head", fuse=False,
      surface="shell")
k.add("brow", ell((0, -0.8, 0.65), (0.27, 0.27, 0.11), rot=(-12, 0, 0), deform=dome), PLATE, "head", surface="shell")

# The shell, its hood over the neck, and the tail it runs back into
k.add("shell", ell((0, 0.04, 0.6), (0.6, 0.72, 0.34), deform=dome), SHELL, "body", surface="shell")
k.add("shellhood", ell((0, -0.5, 0.7), (0.46, 0.3, 0.18), deform=dome), SHELL, "body", surface="shell")
k.add("shelltail", tube([(0, 0.55, 0.55), (0, 0.8, 0.46), (0, 1.0, 0.36)], [0.2, 0.11, 0.02]), SHELL, "body",
      surface="shell")

# The band of rock round the shell: high at the front, low at the back
def band_z(a):
    return 0.66 + 0.11 * np.cos(a)
def band_at(a, out=0.0):
    zb = band_z(a)
    s = math.sqrt(max(0.0, 1 - ((zb - 0.6) / 0.34) ** 2))
    return np.array([(0.6 * s + out) * math.sin(a), 0.04 - (0.72 * s + out) * math.cos(a), zb])
def band_strip(u, v):
    """The band as a broad strip lying on the shell: u round it, v across it, lifted off the shell along its normal."""
    a = 2 * math.pi * u
    z = band_z(a) + (v - 0.5) * 0.13
    rz = np.where(z >= 0.6, 0.34, 0.119)
    s = np.sqrt(np.clip(1 - ((z - 0.6) / rz) ** 2, 0.04, 1))
    x, y = 0.6 * s * np.sin(a), 0.04 - 0.72 * s * np.cos(a)
    n = np.stack([x / 0.36, (y - 0.04) / 0.5184, (z - 0.6) / rz ** 2], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return np.stack([x, y, z], axis=-1) + n * 0.022
# Loose, so its edges stay crisp; the paint under it fills any gap with the same grey
k.add("band", sheet(band_strip, 97, 6, 0.04), ROCK, "body", fuse=False, surface="shell")
def band(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - 0.04))
    return inside(np.abs(P[:, 2] - band_z(a)) - 0.06, 0.008)
k.paint(band, ROCK, on=["shell", "band"])
# The shell's seams above the band and its dark lower edge
def seams(P):
    a = np.arctan2(P[:, 0], -(P[:, 1] - 0.04))
    top = inside(np.abs(P[:, 2] - 0.86) - 0.01, 0.006) * (P[:, 1] > -0.4)
    ribs = sum(inside(np.abs(P[:, 1] - y) - 0.01, 0.006) for y in (-0.12, 0.28)) * (P[:, 2] > band_z(a) + 0.08)
    tail = inside(np.abs(((P[:, 1] - 0.6) % 0.12) - 0.06) - 0.009, 0.006) * (P[:, 1] > 0.62)
    return np.clip(top + ribs + tail, 0, 1)
k.paint(seams, SEAM, on=["shell"])
k.paint(lambda P: inside(P[:, 2] - 0.52, E) * (P[:, 1] < 0.6), SEAM, on=["shell"])

# Legs: brown pillars, wider at the foot, three claws each
for name, y, bone in (("leg", -0.32, "leg{s}"), ("hind", 0.38, "hind{s}")):
    k.add(name, tube([(0.42, y, 0.42), (0.44, y, 0.24), (0.45, y, 0.17)], [0.17, 0.17, 0.18]), LEGS, bone, mirror=True)
    for i, dx in enumerate((-0.09, 0.0, 0.09)):
        k.add(f"claw{name}{i}", tube([(0.45 + dx, y - 0.13, 0.06), (0.45 + dx * 1.25, y - 0.22, 0.015)],
                                     [0.045, 0.012], seg=6, per=3), CLAW, bone, fuse=False, mirror=True,
              surface="shell")

# Rock spikes: on the band's corners, and standing on the shell
def spike(name, base, d, length, r):
    d = np.array(d, float); d /= np.linalg.norm(d)
    base = np.array(base, float)
    k.add(name, tube([base, base + d * length * 0.5, base + d * length], [r, r * 0.6, 0.008], seg=6, per=3), ROCK,
          "body", fuse=False, surface="shell")
for s, sx in (("R", 1), ("L", -1)):
    f = band_at(sx * 0.95)
    spike(f"cornerf{s}", f, (sx, -0.45, 0.12), 0.32, 0.075)
    b = band_at(sx * 2.25)
    spike(f"cornerb{s}", b, (sx, 0.5, 0.25), 0.22, 0.06)
def peak(name, base, d, length, r, seed):
    """A peak of stone: a cone cut into rough flat faces, a mountain rather than a horn."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    def cone(V):
        V = facets(5, 0.9, seed, depth=(0.55, 0.75))(V)
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)                    # 0 at the base, 1 at the tip
        w = (1 - t) ** 1.15
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (r, r * 0.85, length / 2), deform=cone)
    side = np.cross(d, (0, 1.0, 0)); side /= np.linalg.norm(side)
    M = np.stack([side, np.cross(d, side), d], axis=1)
    k.add(name, (V @ M.T + np.array(base, float) + d * length / 2, F), ROCK, "body", fuse=False, surface="shell")
peak("rock1", (-0.3, -0.26, 0.76), (-0.35, -0.2, 1), 0.44, 0.13, 3)
peak("rock2", (0.33, -0.1, 0.76), (0.4, -0.1, 1), 0.36, 0.12, 5)
peak("rock3", (-0.3, 0.34, 0.74), (-0.3, 0.25, 1), 0.44, 0.13, 7)

# The tree: a trunk forking from a patch of soil, a root over the shell, a crown of leafy lumps
k.add("trunk", tube([(0.06, 0.22, 0.82), (0.06, 0.25, 1.0), (0.03, 0.22, 1.12)], [0.14, 0.1, 0.08]), TRUNK, "body",
      surface="shell")
for i, pts in enumerate([[(0.04, 0.23, 1.06), (-0.02, 0.2, 1.22), (-0.06, 0.18, 1.36)],      # to the main crown
                         [(0.07, 0.25, 1.02), (0.24, 0.32, 1.1), (0.4, 0.4, 1.15)],          # to the right
                         [(0.06, 0.27, 1.04), (0.06, 0.46, 1.14), (0.08, 0.6, 1.2)],          # to the back
                         [(0.03, 0.22, 1.06), (-0.16, 0.12, 1.14), (-0.33, 0.02, 1.18)]]):    # to the front left
    k.add(f"branch{i}", tube(pts, [0.065, 0.045, 0.03]), TRUNK, "canopy", surface="shell")
k.add("root", tube([(0.06, 0.2, 0.86), (-0.12, 0.05, 0.9), (-0.3, -0.08, 0.85)], [0.07, 0.05, 0.025]), TRUNK, "body",
      surface="shell")
def soil(P):
    dx, dy = P[:, 0] - 0.12, P[:, 1] - 0.3
    th = np.arctan2(dy, dx)
    r = np.sqrt(dx * dx + dy * dy)
    return inside(r - (0.26 + 0.05 * tri(th * 6 / (2 * math.pi)) + 0.03 * tri(th * 13 / (2 * math.pi))), E) \
        * (P[:, 2] > 0.75)
k.paint(soil, SOIL, on=["shell"])

def leafy(n, m, amp):
    def f(V):
        th = np.arctan2(V[:, 1], V[:, 0]); ph = np.arccos(np.clip(V[:, 2], -1, 1))
        return V * (1 + amp * np.abs(np.sin(n * th) * np.sin(m * ph)))[:, None]
    return f
# Four lobes of foliage, each its own cluster of lumps with gaps of sky between them: the main crown high in the
# middle, a lower one out to the right, one at the back and a small one at the front left
LOBES = [
    [((-0.06, 0.18, 1.44), (0.3, 0.27, 0.15), LEAF), ((-0.22, 0.28, 1.47), (0.17, 0.16, 0.11), LEAF2),
     ((0.08, 0.1, 1.5), (0.17, 0.15, 0.11), LEAF2), ((-0.08, 0.2, 1.56), (0.17, 0.16, 0.08), LEAF)],
    [((0.45, 0.4, 1.17), (0.2, 0.2, 0.11), LEAF3), ((0.5, 0.5, 1.22), (0.13, 0.13, 0.08), LEAF2)],
    [((0.1, 0.64, 1.25), (0.17, 0.15, 0.1), LEAF3), ((0.02, 0.72, 1.28), (0.1, 0.1, 0.07), LEAF)],
    [((-0.38, 0.0, 1.2), (0.13, 0.12, 0.08), LEAF2)],
]
for i, lobe in enumerate(LOBES):
    for j, (c, r, col) in enumerate(lobe):
        k.add(f"crown{i}{j}", ell(c, (r[0] * 1.35, r[1] * 1.35, r[2] * 1.1), deform=leafy(5 + j % 3, 4 + (i + j) % 2, 0.28)), col, "canopy",
              surface="leaf")

# The face: brown under the jaw, the mouth's line, the eyes
k.paint(lambda P: inside(P[:, 2] - (0.44 - 0.12 * np.clip(P[:, 1] + 1.0, 0, 1)), E), BROWN, on=["head"])
k.decal((0, -1.06, 0.44), unit(0, -1, -0.2), (0.13, 0.016), "#22201E", eps=0.004, rings=4, segs=36)

WHITE, IRIS, PUPIL, RIM = srgb("#F6F2EA"), srgb("#D8303A"), srgb("#1A1214"), srgb("#1E3A22")
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[((u + 0.2) / 0.5) ** 2 + (v / 0.6) ** 2 < 1] = IRIS
    c[((u + 0.2) / 0.22) ** 2 + (v / 0.3) ** 2 < 1] = PUPIL
    c[u * u + v * v > 0.78] = RIM
    c[v > 0.55 - 0.35 * u] = RIM                               # the scowl: the brow low over the inner corner
    return c
de = unit(0.8, -0.55, 0.12)
LID = srgb(SHELL)
def eyes(state):
    k.decal(H + np.array([0, -0.03, 0.02]) + de * 0.25, de, (0.06, 0.055), eye_look(state, eye, LID, RIM), eps=0.006,
            rings=20, segs=56, mirror=True)

k.build(voxel=0.017, band=0.026)
k.eye_states(eyes)
k.export(out)
