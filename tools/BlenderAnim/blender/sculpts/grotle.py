# Grotle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/grotle.py -- <the export's Grotle.glb> <out.glb>
#
# The design, from the references: a pale green body on four stout legs; a high yellow shell that comes forward over
# the head as a hood and runs back to a point lifted over the tail, its lower edge a darker ochre lip and its plates
# parted by thin seams; a brown wedge down the middle of the shell's front; two leafy shrubs on its back; a green head
# with a yellow pointed snout, an open mouth and white eyes with black pupils; yellow cuffs on the feet with three white
# claws each; a little green tail under the shell's point.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

GREEN, LEGS, SHELL, LIP, SEAM, STRIPE, YELLOW, LEAF, LEAF2, LEAF3, CLAW = \
    "#A9CF86", "#9CC67A", "#F0C83C", "#C99A2A", "#B58A22", "#9A6A3A", "#F3CB52", "#3E9E4E", "#58B460", "#2F8040", "#F2F0EA"
E = 0.012                                                    # a paint's edge: about a voxel

# The body under the shell, the head before it, the tail behind
k.add("body", ell((0, 0.04, 0.36), (0.31, 0.5, 0.2)), GREEN, "body")
H = np.array([0, -0.6, 0.42])
k.add("head", ell(H, (0.19, 0.23, 0.18), deform=boxy(2.3)), GREEN, "head")
k.add("neck", tube([(0, -0.25, 0.38), (0, -0.48, 0.42)], [0.17, 0.15]), GREEN, "head")
def beak(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)                                 # toward the front the snout comes to a point
    V[:, 0] *= 1 - 0.7 * t; V[:, 2] *= 1 - 0.55 * t
    return V
k.add("snout", ell((0, -0.79, 0.485), (0.075, 0.13, 0.06), rot=(-10, 0, 0), deform=beak), YELLOW, "head")
k.add("tail", tube([(0, 0.46, 0.3), (0, 0.6, 0.24)], [0.07, 0.03]), GREEN, "tail")

# The shell: a dome flattened underneath, running back to a lifted point, and a hood over the head
def dome(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.35, V[:, 2])
    return V
k.add("shell", ell((0, 0.06, 0.5), (0.42, 0.58, 0.26), deform=dome), SHELL, "body", surface="shell")
def point(V):
    V = V.copy()
    t = np.clip(V[:, 1], 0, 1)                                  # toward the back it narrows to a point
    V[:, 0] *= 1 - 0.85 * t ** 1.3
    V[:, 2] = V[:, 2] * (1 - 0.6 * t) + 1.3 * t ** 2            # and lifts
    return V
k.add("shellpoint", ell((0, 0.5, 0.46), (0.3, 0.3, 0.09), deform=point), SHELL, "tail", surface="shell")
k.add("shellhood", ell((0, -0.5, 0.56), (0.25, 0.26, 0.13), rot=(-14, 0, 0), deform=dome), SHELL, "head",
      surface="shell")

# Legs: green pillars in yellow cuffs, three white claws each
for name, y, bone in (("leg", -0.22, "leg{s}"), ("hind", 0.3, "hind{s}")):
    k.add(name, tube([(0.27, y, 0.32), (0.29, y, 0.17), (0.3, y - 0.01, 0.11)], [0.105, 0.1, 0.11]), LEGS, bone,
          mirror=True)
    for i, dx in enumerate((-0.06, 0.0, 0.06)):
        k.add(f"claw{name}{i}", tube([(0.3 + dx, y - 0.075, 0.04), (0.3 + dx * 1.25, y - 0.135, 0.012)],
                                     [0.026, 0.008], seg=8, per=4), CLAW, bone, fuse=False, mirror=True,
              surface="shell")
k.paint(lambda P: inside(P[:, 2] - 0.13, E), YELLOW, on=["leg", "hind"])

# Two shrubs: a fused crown each, and flat pointed leaves loose round it for the leafy, spiky outline
def leaf(base, d, length, width, thick=0.014):
    """A flat leaf from base along d, pointed at its tip, its face turned to the sky's side of d."""
    d = d / np.linalg.norm(d)
    side = np.cross(d, (0, 0, 1.0))
    if np.linalg.norm(side) < 1e-3:
        side = np.array([1.0, 0, 0])
    side /= np.linalg.norm(side)
    face = np.cross(side, d)
    def shape(V):
        V = V.copy()
        t = (V[:, 2] + 1) / 2                                   # 0 at the base, 1 at the tip
        w = np.sqrt(np.clip(t, 0, 1)) * (1 - t ** 1.6) * 1.6
        V[:, 0] *= w; V[:, 1] *= w
        return V
    V, F = ell((0, 0, 0), (width, thick, length / 2), deform=shape, sub=2)
    M = np.stack([side, face, d], axis=1)                       # local x, y, z to the world's
    return V @ M.T + base + d * length / 2, F

# Each shrub an irregular clump: lobes of differing size scattered about its bone, their surfaces shaggy, and a few
# loose leaves in tufts standing out of the outline. The two are not mirror images: the right one sits further back
# and spreads lower, the left one further forward and stands taller, as in the pictures.
from sculptkit import lumpy
SHRUBS = {"R": dict(c=(0.2, 0.14, 0.72), size=1.18, seed=11), "L": dict(c=(-0.19, -0.02, 0.76), size=1.05, seed=23)}
for s, sh in SHRUBS.items():
    sx = 1 if s == "R" else -1
    rs = np.random.RandomState(sh["seed"])
    c0 = np.array(sh["c"]); g = sh["size"]
    k.add(f"shrub{s}0", ell(c0, (0.19 * g, 0.21 * g, 0.15 * g), deform=lumpy(7, 5, 0.12)), LEAF3, f"shrub{s}",
          surface="leaf")
    lobes = [(c0 + np.array([0, 0, 0.04 * g]), 0.16 * g)]
    for i in range(6):
        az = 2 * math.pi * i / 6 + rs.uniform(-0.5, 0.5)
        far = rs.uniform(0.1, 0.17) * g
        up = rs.uniform(0.0, 0.13) * g
        r = rs.uniform(0.07, 0.13) * g
        c = c0 + np.array([far * math.cos(az), far * math.sin(az) * 1.15, up])
        c[0] = sx * max(sx * c[0], 0.05)                        # keep each shrub on its own side of the wedge
        lobes.append((c, r))
        col = (LEAF, LEAF2, LEAF3)[rs.randint(3)]
        k.add(f"shrub{s}{i + 1}", ell(c, (r, r * 1.05, r * 0.85), deform=lumpy(5 + i % 3, 4 + i % 2, 0.15)), col,
              f"shrub{s}", surface="leaf")
    # Pointed leaves over every lobe, fanned out the way the lobe lies from the clump's middle (all pointing up they
    # read as grass on balls of moss), of differing lengths: a shaggy outline
    n = 0
    for c, r in lobes:
        for j in range(rs.randint(7, 11)):
            out_ = (c - c0) * np.array([1, 1, 0]); out_ = out_ / max(0.04, np.linalg.norm(out_))   # the lobe's way out
            d = rs.normal(size=3) * 0.7 + out_ * 1.1; d[2] = max(d[2], -0.25) + 0.35   # fanned out, a few level or drooping
            d /= np.linalg.norm(d)
            base = c + d * r * 0.55
            if base[2] < c0[2] - 0.08:                          # none under the clump, into the shell
                continue
            col = (LEAF, LEAF2, LEAF3)[rs.randint(3)]
            k.add(f"blade{s}{n}", leaf(base, d, rs.uniform(0.09, 0.19) * g,
                                       rs.uniform(0.05, 0.075)), col, f"shrub{s}", fuse=False, surface="leaf")
            n += 1

# The shell's markings: the darker lip at its edge, thin seams between its plates, the brown wedge down its front
k.paint(lambda P: inside(P[:, 2] - 0.38, E) * (P[:, 1] > -0.42), LIP, on=["shell"])
k.paint(lambda P: inside(P[:, 2] - 0.47, E) * (P[:, 1] <= -0.42), LIP, on=["shellhood", "shell"])
def seams(P):
    ring = inside(np.abs(P[:, 2] - 0.5) - 0.007, 0.004) * (P[:, 1] > -0.38)                 # a plate round the edge
    ribs = sum(inside(np.abs(P[:, 1] - y) - 0.007, 0.004) for y in (-0.3, 0.12, 0.42)) * (P[:, 2] > 0.4) \
        * (P[:, 2] < 0.6)
    return np.clip(ring + ribs, 0, 1)
k.paint(seams, SEAM, on=["shell", "shellpoint", "shellhood"])
def wedge(P):
    t = np.clip((P[:, 1] + 0.62) / 0.6, 0, 1)                          # 0 at the hood's front, 1 among the shrubs
    half = 0.02 + 0.1 * t
    return inside(np.abs(P[:, 0]) - half, E) * (P[:, 1] < -0.02) * (P[:, 2] > 0.52)
k.paint(wedge, STRIPE, on=["shell"])

# The mouth, open under the snout
MOUTH, TONGUE, INK = srgb("#7A2A2A"), srgb("#E4808A"), srgb("#2A2420")
def mouth(u, v):
    c = np.tile(MOUTH, (len(u), 1))
    c[v < -0.15] = TONGUE
    c[u * u + v * v > 0.72] = INK
    return c
k.decal((0, -0.79, 0.37), unit(0, -1, -0.15), (0.085, 0.04), mouth, eps=0.004, rings=8, segs=36,
        outline=lambda th: 1.0 if math.sin(th) < 0 else 0.55)

# The eyes: white, a black pupil toward the front, a dark rim
WHITE, PUPIL, RIM = srgb("#FFFFFF"), srgb("#1C1A18"), srgb("#3A3428")
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[((u + 0.28) / 0.45) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = PUPIL
    c[((u + 0.2) / 0.16) ** 2 + ((v - 0.22) / 0.16) ** 2 < 1] = WHITE
    c[u * u + v * v > 0.8] = RIM
    return c
de = unit(0.86, -0.48, 0.12)
LID = srgb(GREEN)
def eyes(state):
    k.decal(H + np.array([0, -0.02, 0.02]) + de * 0.19, de, (0.05, 0.058), eye_look(state, eye, LID, RIM), eps=0.005,
            rings=20, segs=56, mirror=True)

k.build(voxel=0.013, band=0.018)
k.eye_states(eyes)
k.export(out)
