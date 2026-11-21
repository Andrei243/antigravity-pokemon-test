# Mega Glalie sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/glaliemega.py -- <the export's Glalie-Mega.glb> <out.glb>
# Grown from glalie.py (which builds as it is read, so its way is copied here): the same black rock, white ice, pale
# blue eyes in black sockets, faceted horns and flat white teeth with grey lines between them. From the pictures, the
# masses largest first:
#   1. its jaw broken open on a great round dark-blue maw filling the front of its lower half, ringed by a rim of black
#      rock whose outer edge is rough with points, a band of flat white teeth round the maw's lower half;
#   2. a skull of white ice over the upper half (no lattice now), heavy brows, its lower edge hanging in cheekbones at
#      the sides and a row of flat upper teeth in the middle over the maw;
#   3. angry cyan eyes in black sockets under black slanting brows, red slit pupils;
#   4. two pairs of horns: cyan crystal on the brow, rising up and out, and black ones further back at the upper corners
#      with a cyan stripe down each;
#   5. small pale ice spikes low at the sides.
# The body is taller than wide (an egg); the skeleton is one bone, the body, at z 0.354.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, polygon, eye_look, inside, smoothstep, hull, args

src, dst = args()
k = Kit(src, coat="shell")

ICE, ROCK, MAW, WHITE, LINE = "#E6ECF6", "#2A2C34", "#163A72", "#F4F4F0", "#8E939C"
CYAN, CYANLT, BLUE = "#5CC8EE", "#A6E4F8", "#4CC4F0"
C = np.array([0.0, 0.0, 0.385])         # the egg's middle
R = np.array([0.262, 0.24, 0.29])       # its half sizes
MC = np.array([0.0, 0.3])               # the maw's middle in (x, z)
MR = np.array([0.185, 0.19])            # its half sizes in x and z


def front_y(x, z):
    """Where the egg's front surface is at (x, z)."""
    q = 1 - (x / R[0]) ** 2 - ((z - C[2]) / R[2]) ** 2
    return -R[1] * math.sqrt(max(q, 0.0))


# --- the rock egg, and the ice skull's ridges fused on it ---------------------------------------------------------
k.add("rock", ell(C, R, sub=5), ROCK, "body")
for s in (1, -1):
    # the brows: heavy ridges over the eyes, slanting down toward the middle
    k.add(f"brow{'R' if s > 0 else 'L'}", ell((0.1 * s, front_y(0.1, 0.58) + 0.03, 0.585), (0.085, 0.045, 0.04),
                                              rot=(0, -18 * s, 0)), ICE, "body")
    # the cheekbones hanging at the mask's lower sides
    k.add(f"cheek{'R' if s > 0 else 'L'}", ell((0.2 * s, front_y(0.2, 0.42) + 0.035, 0.42), (0.055, 0.045, 0.07)),
          ICE, "body")
# the ridge down between the eyes to the teeth
k.add("nose", ell((0, front_y(0, 0.5) + 0.02, 0.5), (0.035, 0.03, 0.07)), ICE, "body")

# the rim of rock round the maw, standing forward of the egg
RIM = []
for a in np.linspace(0, 2 * math.pi, 49):
    x, z = MC[0] + (MR[0] + 0.02) * math.sin(a), MC[1] - (MR[1] + 0.02) * math.cos(a)
    if z > 0.45:
        z = 0.45
    RIM.append((x, front_y(x, z) - 0.008, z))
k.add("rim", tube(RIM, [0.032] * len(RIM), seg=12, per=3), ROCK, "body")


# --- paint: the ice skull over the top half, the maw ---------------------------------------------------------------
def skull(P):
    Q = P - C
    ax = np.abs(Q[:, 0])
    fr = smoothstep(-0.05, 0.1, -Q[:, 1])
    b = 0.07 - 0.075 * np.exp(-((ax - 0.2) / 0.05) ** 2) * fr + 0.05 * (1 - fr)
    return inside(b - Q[:, 2], 0.004)
k.paint(skull, ICE)
k.paint(lambda P: inside(((P[:, 0] - MC[0]) / MR[0]) ** 2 + ((P[:, 2] - MC[1]) / MR[1]) ** 2 - 1, 0.02)
        * inside(P[:, 1] + 0.08, 0.01) * inside(P[:, 2] - 0.445, 0.004), MAW, on=["rock"])

# --- teeth: a row of flat upper teeth under the skull's middle, a band round the maw's lower half -----------------
def tooth(c, d, w, h, t=0.022):
    """A flat tooth: a slab of w by h facing along d (outward), its corners squared."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)
    pts = []
    for a in (-1, 1):
        for b in (-1, 1):
            for e in (-1, 1):
                pts.append(np.array(c) + side * a * w / 2 + up * b * h / 2 + d * e * t / 2)
    return hull(np.array(pts))

for i, x in enumerate(np.linspace(-0.1, 0.1, 6)):
    z = 0.425
    k.add(f"uptooth{i}", tooth((x, front_y(x, z) - 0.012, z), (0, -1, -0.15), 0.039, 0.052), WHITE, "body",
          fuse=False, surface="shell")
for i, a in enumerate(np.linspace(-1.75, 1.75, 11)):
    x, z = MC[0] + 0.86 * MR[0] * math.sin(a), MC[1] - 0.86 * MR[1] * math.cos(a)
    out_ = np.array([math.sin(a), 0, -math.cos(a)])
    d = unit(0, -1, 0) * 0.85 - out_ * 0.15
    k.add(f"lowtooth{i}", tooth((x, front_y(x, z) - 0.008, z), d, 0.05, 0.056, 0.02), WHITE, "body",
          fuse=False, surface="shell")

# --- the rough outer edge of the rim: black points round the sides and the bottom ---------------------------------
for i, a in enumerate(np.linspace(0.75, 2 * math.pi - 0.75, 15)):
    # a: round from straight down; the points stand out of the egg's sides in the plane of the maw
    dx, dz = math.sin(a), -math.cos(a)
    if dz > 0.55:
        continue
    root = np.array([dx * R[0] * 0.92, -0.08, C[2] + dz * R[2] * 0.85])
    tip = root + np.array([dx, -0.25, dz * 0.8]) * 0.075
    side = unit(-dz, 0, dx)
    pts = [tip] + [root + side * a_ * 0.035 + np.array([0, b_ * 0.03, 0]) for a_ in (-1, 1) for b_ in (-1, 1)]
    k.add(f"point{i}", hull(np.array(pts)), ROCK, "body", fuse=False, surface="shell")

# --- the horns -------------------------------------------------------------------------------------------------------
def horn(root, tip, r0, flat=0.6, sides=6):
    root, tip = np.array(root, float), np.array(tip, float)
    d = tip - root; d /= np.linalg.norm(d)
    s1 = np.cross(d, (0, 1.0, 0)); s1 /= np.linalg.norm(s1)
    s2 = np.cross(d, s1)
    pts = [tip]
    for ring, rr in ((0.0, r0), (0.3, r0 * 0.8)):
        base = root + (tip - root) * ring
        for j in range(sides):
            a = 2 * math.pi * (j + 0.5 * ring) / sides
            pts.append(base + rr * (math.cos(a) * s1 + flat * math.sin(a) * s2))
    return hull(np.array(pts))

for s in (1, -1):
    sd = "R" if s > 0 else "L"
    # the crystal horns on the brow
    k.add(f"crystal{sd}", horn((0.1 * s, -0.12, 0.6), (0.2 * s, -0.08, 0.8), 0.05), CYAN, "body", fuse=False,
          surface="shell")
    k.add(f"crystallt{sd}", horn((0.115 * s, -0.135, 0.63), (0.19 * s, -0.105, 0.77), 0.024, flat=0.5), CYANLT,
          "body", fuse=False, surface="shell")
    # the black horns at the upper corners, a cyan stripe down their front
    k.add(f"horn{sd}", horn((0.19 * s, 0.04, 0.56), (0.3 * s, 0.09, 0.8), 0.055), ROCK, "body", fuse=False,
          surface="shell")
    k.add(f"stripe{sd}", horn((0.215 * s, 0.0, 0.6), (0.29 * s, 0.05, 0.75), 0.02, flat=0.4), CYAN, "body",
          fuse=False, surface="shell")
    # the small ice spikes low at the sides
    k.add(f"spike{sd}", horn((0.2 * s, 0.0, 0.2), (0.29 * s, -0.03, 0.1), 0.04), ICE, "body", fuse=False,
          surface="shell")

# --- the brows' black marks over the eyes, slanting down to the middle -----------------------------------------------
for s in (1, -1):
    out_l = [(-1, -0.6), (1, 0.2), (0.9, 0.6), (-1, -0.15)]
    pts = [(a * s, b) for a, b in out_l]
    c0 = np.array([0.105 * s, 0, 0.575]); c0[1] = front_y(0.105, 0.575) - 0.02
    k.decal(c0, unit(0.3 * s, -0.9, 0.3), (0.075, 0.03), ROCK, eps=0.005, rings=10, segs=48,
            up=(0, 0, 1), outline=polygon(pts) if s > 0 else polygon([(-a, b) for a, b in out_l]))

body = k.build(voxel=0.0062, smooth=5, band=0.003)

# --- the eyes: cyan in black sockets, cut on a slope by the brow, red slit pupils ------------------------------------
DK, CY, RED, PALE = srgb(ROCK), srgb(BLUE), srgb("#D8303A"), srgb("#BCE6F8")
def brow(u):
    return 0.22 + 0.42 * u
def eye(u, v):
    c = np.tile(DK, (len(u), 1))
    cy = ((u / 0.78) ** 2 + ((v + 0.06) / 0.66) ** 2 < 1) & (v < brow(u))
    c[cy] = CY
    c[cy & (((u - 0.3) / 0.13) ** 2 + ((v - 0.04) / 0.13) ** 2 < 1)] = (1, 1, 1)
    return c
def look(state, flip):
    inner = eye_look(state, eye, DK, PALE, line=0.18)
    return (lambda u, v: inner(-u, v)) if flip else inner
def eyes(state):
    for s in (1, -1):
        x, z = 0.1, 0.515
        c0 = np.array([x * s, front_y(x, z), z])
        n = unit(0.38 * s, -0.88, 0.2)
        k.decal(c0, n, (0.05, 0.042), look(state, s < 0), eps=0.003, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the slit pupil, a patch of its own
            k.decal(c0 + n * 0.002 + np.array([-0.006 * s, 0, -0.004]), n, (0.006, 0.026), RED, eps=0.0055,
                    rings=6, segs=28)

k.eye_states(eyes)
k.export(dst)
