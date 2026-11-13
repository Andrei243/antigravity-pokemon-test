# Bonsly sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bonsly.py -- <the export's Bonsly.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="leaf")

BROWN, RIM, GREEN, YELLOW, INK, LIP = "#C48A55", "#B27A47", "#46AE5E", "#F2D14C", "#24180E", "#D88A86"


def torus(c, R, r, seg=64, ring=20):
    """A ring lying flat about an upright axis."""
    c = np.array(c, float)
    V = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        for j in range(ring):
            b = 2 * math.pi * j / ring
            rr = R + r * math.cos(b)
            V.append(c + (rr * math.cos(a), rr * math.sin(a), r * math.sin(b)))
    F = []
    for i in range(seg):
        for j in range(ring):
            a, b = i * ring + j, i * ring + (j + 1) % ring
            a2, b2 = ((i + 1) % seg) * ring + j, ((i + 1) % seg) * ring + (j + 1) % ring
            F.append([a, a2, b2, b])
    return np.array(V), F


# The pot: a round bowl under a thick rim
k.add("bowl", ell((0, 0.0, 0.145), (0.125, 0.115, 0.1)), BROWN, "body")
k.add("rim", torus((0, 0, 0.205), 0.13, 0.036), RIM, "body")
# The trunk rising out of the pot as a teardrop, narrowing to its stalk
def drop(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    s = 1 - 0.82 * t ** 1.25
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("trunk", ell((0, 0.0, 0.29), (0.135, 0.125, 0.25), deform=drop), BROWN, "body")

# The stalk and its branches, brown, with a little knot where they part
k.add("stalk", tube([(0, 0.005, 0.47), (0, 0.005, 0.55), (0, 0.005, 0.63)], [0.034, 0.026, 0.024]), BROWN, "head")
k.add("knot", ell((0, 0.005, 0.565), (0.036, 0.026, 0.018)), BROWN, "head")
k.add("branch", tube([(0, 0.005, 0.565), (0.05, 0.008, 0.585), (0.085, 0.01, 0.6)], [0.02, 0.018, 0.017]), BROWN,
      "head", mirror=True)

# The three round crowns of leaves, smooth balls: one on top, one each side
k.add("crown", ell((0, 0.005, 0.7), (0.085, 0.083, 0.083)), GREEN, "head", fuse=False, surface="leaf")
k.add("side", ell((0.13, 0.012, 0.625), (0.078, 0.076, 0.076)), GREEN, "head", fuse=False, mirror=True, surface="leaf")

# Short stub legs on oval feet
k.add("ankle", tube([(0.065, -0.01, 0.1), (0.072, -0.03, 0.05)], [0.03, 0.022]), BROWN, "leg{s}", mirror=True)
k.add("foot", ell((0.075, -0.045, 0.032), (0.048, 0.068, 0.032)), BROWN, "leg{s}", mirror=True)

# The rim's own shade, a crisp band round the trunk's foot
def rim(P):
    r = np.sqrt(P[:, 0] ** 2 + P[:, 1] ** 2)
    return inside(np.abs(P[:, 2] - 0.205) - 0.034, 0.004) * (r > 0.12)
k.paint(rim, RIM)

# The yellow oval on its forehead
k.decal((0, -0.11, 0.36), unit(0, -1, 0.3), (0.024, 0.04), YELLOW, eps=0.003, rings=8, segs=36)

# The frown: a little pink mouth, its line curving down, between the eyes just over the rim
CB, CI, CL = srgb(BROWN), srgb(INK), srgb(LIP)
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    mid = -0.15 + 0.65 * (1 - u * u)
    lips = (np.abs(v - mid + 0.05) < 0.42) & (np.abs(u) < 0.92)
    c[lips] = CL
    c[(np.abs(v - mid) < 0.24) & (np.abs(u) < 0.95)] = CI
    return c
k.decal((0, -0.13, 0.262), unit(0, -1, 0.1), (0.036, 0.02), mouth, eps=0.003, rings=16, segs=64)

# The eyes: yellow ovals in a dark rim, a tall black pupil in each
CY = srgb(YELLOW)
def eye(u, v):
    r = (u / 0.95) ** 2 + (v / 0.97) ** 2
    c = np.tile(CB, (len(u), 1))
    c[r < 1] = CI
    c[r < 0.76] = CY
    c[((u + 0.04) / 0.3) ** 2 + ((v + 0.02) / 0.55) ** 2 < 1] = CI
    return c
de = unit(0.5, -0.86, 0.12)
E = np.array([0.064, -0.1, 0.292])
def eyes(state):
    k.decal(E, de, (0.027, 0.041), eye_look(state, eye, CB, CI), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
