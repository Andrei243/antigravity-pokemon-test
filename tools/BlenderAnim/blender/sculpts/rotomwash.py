# Rotom-Wash sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotomwash.py -- <the export's Rotom-Wash.glb> <out.glb>
#
# Grown from rotom.py (and made as rotomheat.py is): the same orange, an opaque aura set BEHIND the core so it shows
# as a rim and leaves the face free (for a box, a thin rounded slab behind its back half), the core copied behind so
# it shows through the aura from the back, the same spike.
# The design, from the references: Rotom inside a front-loading washing machine, a tall rounded orange box with a
# little peak at each top corner beside the spike; its eyes high up, white with a big blue iris, a wide grin with its
# teeth under them; a large round door on the front, a blue glass disc in a dark ring broken into a C on its left
# side (+X); two stubby feet; an orange hose out of its lower right side (-X) ending in a dark nozzle; a blue aura
# round it all, one arm a bolt hanging down beside the hose and the other a bolt ending in an open clamp.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, boxy, slab, outward, lathe

ORANGE, AURA = "#F27028", "#2CA6DE"
WHITE, IRIS, INK, MOUTH = "#FFFFFF", "#22B4E6", "#1A2C5A", "#5A1E14"
RING, GLASS, SHINE, NOZZLE = "#28262E", "#2A9EDC", "#8ED8F4", "#2A2830"

C = np.array([0.0, 0.0, 0.29])            # the machine's middle
R = np.array([0.13, 0.112, 0.158])        # its half sizes
N = 3.6                                   # how square it is
BACK = 0.02                               # the plane the core is mirrored about for its copy behind
SLAB_Y, SLAB_R = 0.09, 0.045              # the aura's slab: its middle behind the machine's, and its half depth


def on_box(d, lift=0.0, c=C, r=R, n=N):
    """The point of the rounded box along a direction from its middle."""
    d = unit(*d)
    s = 1 / (np.abs(d / r) ** n).sum() ** (1 / n)
    return c + d * (s + lift)


def box(c=C, r=R, n=N, sub=5):
    return ell(c, r, deform=boxy(n), sub=sub)


def offset_outline(line, widths, longest=3.0):
    """A closed outline round a polyline, each point its half-width, its corners mitred to points (rotom.py's)."""
    line = np.array(line, float)
    n = len(line)
    seg = [(line[i + 1] - line[i]) / np.linalg.norm(line[i + 1] - line[i]) for i in range(n - 1)]
    left = lambda t: np.array([-t[1], t[0]])
    L, Rr = [], []
    for i in range(n):
        w = widths[i]
        if i == 0 or i == n - 1:
            t = seg[0] if i == 0 else seg[-1]
            m = left(t); kk = 1.0
            p = line[i] + (-t if i == 0 else t) * w * 0.3
        else:
            m = left(seg[i - 1]) + left(seg[i]); m /= np.linalg.norm(m)
            kk = min(1 / max(np.dot(m, left(seg[i])), 1e-3), longest)
            p = line[i]
        L.append(p + m * w * kk); Rr.append(p - m * w * kk)
    return [tuple(p) for p in L + Rr[::-1]]


def behind(shape):
    """The core mirrored front to back, so it shows through the aura from the back."""
    V, F = shape
    V = V.copy(); V[:, 1] = 2 * BACK - V[:, 1]
    return outward((V, F))


src, out = args()
k = Kit(src, coat="shell")

# --- the orange core: the machine, its corner peaks, the spike, the feet, the hose (fused) ---------------------
k.add("machine", box(), ORANGE, "body")
TOP = C[2] + R[2]
k.add("peak", tube([(0.085, 0.0, TOP - 0.05), (0.108, 0.0, TOP - 0.008), (0.12, 0.0, TOP + 0.012)],
                   [0.042, 0.026, 0.008]), ORANGE, "body", mirror=True)
SPB = np.array([-0.008, 0.008, TOP - 0.02])
SPT = np.array([-0.03, 0.016, TOP + 0.13])
SPM = (SPB + SPT) / 2 + np.array([0.004, 0, 0])
k.add("spike", tube([SPB, SPM, SPT], [0.044, 0.026, 0.006]), ORANGE, "body")
k.add("foot", ell((0.065, -0.01, C[2] - R[2] + 0.004), (0.042, 0.042, 0.034)), ORANGE, "body", mirror=True)
HOSE = [np.array(p) for p in ((-0.1, -0.03, 0.18), (-0.16, -0.05, 0.13), (-0.225, -0.065, 0.105),
                              (-0.285, -0.075, 0.118))]
k.add("hose", tube(HOSE, [0.024, 0.022, 0.022, 0.022]), ORANGE, "armL")

# --- the aura: a blue slab behind, and a blue rim round the spike, the peaks and the hose -----------------------
AB = np.array([0, 0.022, 0])
AS = np.array([0, SLAB_Y, 0])
k.add("auramachine", box(C + AS + np.array([0, 0, -0.002]), (R[0] * 1.15, SLAB_R, R[2] * 1.12)), AURA, "body",
      fuse=False, surface="glow")
ext = SPT + (SPT - SPB) / np.linalg.norm(SPT - SPB) * 0.022
k.add("auraspike", tube([SPB + AB, SPM + AB * 0.8, SPT + AB * 0.6, ext + AB * 0.4], [0.054, 0.036, 0.014, 0.003]),
      AURA, "body", fuse=False, surface="glow")
k.add("aurapeak", tube([(0.085, 0.016, TOP - 0.05), (0.112, 0.016, TOP - 0.002), (0.128, 0.016, TOP + 0.026)],
                       [0.054, 0.034, 0.003]), AURA, "body", fuse=False, mirror=True, surface="glow")
k.add("aurahose", tube([p + np.array([0, 0.014, -0.002]) for p in HOSE[1:]] + [HOSE[-1] + (-0.008, 0.014, 0.002)],
                       [0.031, 0.031, 0.031, 0.031]), AURA, "armL", fuse=False, surface="glow")
k.add("backmachine", behind(box()), ORANGE, "body", fuse=False)

# The nozzle: a dark ring with its bore at the hose's end
nd = unit(*(HOSE[-1] - HOSE[-2]))
k.add("nozzle", lathe(HOSE[-1] - nd * 0.006, nd, [(0, 0), (0.027, 0), (0.027, 0.024), (0.014, 0.024),
                                                  (0.014, 0.006), (0, 0.006)], seg=32), NOZZLE, "armL", fuse=False)

# --- the arms: blue bolts in the front's plane; one hangs down beside the hose, the other ends in a clamp ----------
AX, AZ = np.array([1.0, 0, 0]), np.array([0, 0, 1.0])
O = np.array([0.0, -0.01, 0.0])
right = [(0.11, 0.305), (0.19, 0.275), (0.172, 0.21), (0.222, 0.165), (0.236, 0.13)]
left = [(-0.11, 0.305), (-0.188, 0.27), (-0.168, 0.2), (-0.212, 0.14), (-0.198, 0.075)]
k.add("boltR", slab(offset_outline(right, [0.015, 0.016, 0.016, 0.016, 0.016]), O, AX, AZ, 0.022), AURA, "armR",
      fuse=False, surface="glow")
k.add("boltL", slab(offset_outline(left, [0.015, 0.016, 0.016, 0.016, 0.004]), O, AX, AZ, 0.022), AURA, "armL",
      fuse=False, surface="glow")
# the clamp: a thick ring open toward the outside and down
CC = np.array([0.252, 0.0, 0.1])
a0, gap = math.radians(-35), math.radians(80)
outer = [(0.044 * math.cos(a), 0.044 * math.sin(a)) for a in np.linspace(a0 + gap / 2, a0 + 2 * math.pi - gap / 2, 22)]
inner = [(0.019 * math.cos(a), 0.019 * math.sin(a)) for a in np.linspace(a0 + 2 * math.pi - gap / 2, a0 + gap / 2, 16)]
k.add("clamp", slab(outer + inner, CC + O, AX, AZ, 0.026), AURA, "armR", fuse=False, surface="glow")

# --- the door: a blue glass disc in a dark ring broken into a C on its left side (+X) ----------------------------
CR_, CG, CS, CO = srgb(RING), srgb(GLASS), srgb(SHINE), srgb(ORANGE)


def door(u, v):
    r = np.sqrt(u * u + v * v)
    a = np.arctan2(v, u)
    c = np.tile(CR_, (len(u), 1))
    c[r < 0.64] = CG
    c[((u + 0.24) / 0.2) ** 2 + ((v - 0.26) / 0.15) ** 2 < 1] = CS
    gap = (np.abs(a) < 0.34) & (r > 0.6)
    c[gap & (r > 0.93)] = CO
    c[gap & (r <= 0.93)] = CG
    c[(np.abs(a) < 0.15) & (r > 0.52) & (r < 0.8)] = CR_     # a dark tab in the gap, the latch
    return c


dd = unit(0, -1, -0.58)
k.decal(on_box(dd), (0, -1, 0), (0.082, 0.082), door, eps=0.004, rings=24, segs=96)

# --- the grin: a wide smile with its teeth showing -------------------------------------------------------------
CM, CW = srgb(MOUTH), srgb(WHITE)


def grin(u, v):
    c = np.tile(CO, (len(u), 1))
    up = 0.3 + 0.45 * u * u
    lo = -0.7 + 0.95 * u * u
    m = (v < up) & (v > lo) & (np.abs(u) < 0.92)
    c[m] = CM
    c[m & (v > up - 0.55)] = CW
    return c


dm = unit(0, -1, 0.4)
k.decal(on_box(dm), (0, -1, 0.05), (0.042, 0.02), grin, eps=0.004, rings=16, segs=64)

k.build(voxel=0.005)

# Eyes: white with a big blue iris, a dark pupil, a glint, a thin dark edge (rotom.py's)
CI, CK = srgb(IRIS), srgb(INK)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u - 0.04) / 0.56) ** 2 + ((v + 0.04) / 0.66) ** 2 < 1] = CI
    c[((u - 0.04) / 0.24) ** 2 + ((v + 0.04) / 0.32) ** 2 < 1] = CK
    c[((u + 0.12) / 0.17) ** 2 + ((v - 0.28) / 0.17) ** 2 < 1] = CW
    c[np.sqrt(u * u + v * v) > 0.84] = CK
    return c


de = unit(0.4, -1, 0.9)
E = on_box(de)


def eyes(state):
    k.decal(E, (0.06, -1, 0.05), (0.028, 0.031), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20,
            segs=56, up=(-0.15, 0, 1), mirror=True)


k.eye_states(eyes)
k.export(out)
