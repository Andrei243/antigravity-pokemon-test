# Rotom-Frost sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotomfrost.py -- <the export's Rotom-Frost.glb> <out.glb>
#
# Grown from rotom.py (and made as rotomheat.py is): the same orange, an opaque aura set BEHIND the core so it shows
# as a rim and leaves the face free (for a box, a thin rounded slab behind its back half), the core copied behind so
# it shows through the aura from the back, the same spike.
# The design, from the references: Rotom inside a refrigerator, a tall orange box with rounded edges; on its upper
# door two eyes, white with a pale violet iris, and a small frowning mouth; a dark seam under the upper door, a
# middle section split by a vertical seam with two raised oval handles either side of it, a second seam, and under
# it a dark half-disc; a pale violet aura round it all with two jagged lightning wings out and up to the sides.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, unit, eye_look, args, boxy, slab, outward, polygon

ORANGE, HANDLE, AURA = "#F27028", "#F8935A", "#9C96DA"
WHITE, IRIS, PUPIL, INK, SEAM = "#FFFFFF", "#B6AAEC", "#4A3C8A", "#2A2050", "#2A1A18"

C = np.array([0.0, 0.0, 0.27])            # the fridge's middle
R = np.array([0.11, 0.1, 0.175])          # its half sizes
N = 4.2                                   # how square it is
BACK = 0.02                               # the plane the core is mirrored about for its copy behind
SLAB_Y, SLAB_R = 0.075, 0.042             # the aura's slab: its middle behind the fridge's, and its half depth
FY = C[1] - R[1]                          # the front


def on_box(d, lift=0.0, c=C, r=R, n=N):
    """The point of the rounded box along a direction from its middle."""
    d = unit(*d)
    s = 1 / (np.abs(d / r) ** n).sum() ** (1 / n)
    return c + d * (s + lift)


def box(c=C, r=R, n=N, sub=5):
    return ell(c, r, deform=boxy(n), sub=sub)


def behind(shape):
    """The core mirrored front to back, so it shows through the aura from the back."""
    V, F = shape
    V = V.copy(); V[:, 1] = 2 * BACK - V[:, 1]
    return outward((V, F))


src, out = args()
k = Kit(src, coat="shell")

# --- the orange core: the fridge, its spike (fused), the two handles (loose) ----------------------------------------
k.add("fridge", box(), ORANGE, "body")
TOP = C[2] + R[2]
SPB = np.array([-0.008, 0.008, TOP - 0.02])
SPT = np.array([-0.034, 0.016, TOP + 0.13])
SPM = (SPB + SPT) / 2 + np.array([0.004, 0, 0])
k.add("spike", tube([SPB, SPM, SPT], [0.044, 0.026, 0.006]), ORANGE, "body")
k.add("handle", outward(ell((0.027, FY + 0.004, 0.275), (0.015, 0.011, 0.038), sub=3)), HANDLE, "body", fuse=False, mirror=True)

# --- the aura: a pale violet slab behind, and a rim round the spike ---------------------------------------------
AB = np.array([0, 0.022, 0])
AS = np.array([0, SLAB_Y, 0])
k.add("aurafridge", box(C + AS, (R[0] * 1.17, SLAB_R, R[2] * 1.1)), AURA, "body", fuse=False, surface="glow")
ext = SPT + (SPT - SPB) / np.linalg.norm(SPT - SPB) * 0.022
k.add("auraspike", tube([SPB + AB, SPM + AB * 0.8, SPT + AB * 0.6, ext + AB * 0.4], [0.054, 0.036, 0.014, 0.003]),
      AURA, "body", fuse=False, surface="glow")
k.add("backfridge", behind(box()), ORANGE, "body", fuse=False)

# --- the wings: jagged bolts of lightning out and up from the shoulders, turned a little back -------------------
WING = [(-0.03, 0.04), (-0.005, -0.025), (0.06, -0.12), (0.12, -0.07), (0.26, 0.16), (0.1, 0.045),
        (0.075, -0.035), (0.035, 0.02)]
ROOTS = {"R": np.array([0.08, 0.04, 0.315]), "L": np.array([-0.08, 0.04, 0.325])}
for side, s in (("R", 1), ("L", -1)):
    t = math.radians(22)
    ex = np.array([math.cos(t) * s, math.sin(t), 0.0])        # outward and a little back
    k.add("wing" + side, slab([(a, b) for a, b in WING], ROOTS[side], ex, (0, 0, 1.0), 0.03), AURA, "wing" + side,
          fuse=False, surface="glow")

# --- the front: seams, the dark half-disc and the small frown (patches, before the build) ---------------------------
CO, CS = srgb(ORANGE), srgb(SEAM)
SEAM1, SEAM2 = 0.348, 0.198
rect = lambda a, b: polygon([(-1, -1), (1, -1), (1, 1), (-1, 1)])
for z in (SEAM1, SEAM2):
    d = unit(0, -1, (z - C[2]) / R[1])
    k.decal(on_box(d), (0, -1, 0), (0.1, 0.0045), CS, eps=0.004, outline=rect(1, 1), rings=10, segs=96)
dv = unit(0, -1, ((SEAM1 + SEAM2) / 2 - C[2]) / R[1])
k.decal(on_box(dv), (0, -1, 0), (0.0045, (SEAM1 - SEAM2) / 2), CS, eps=0.0042, outline=rect(1, 1), rings=10,
        segs=96)
# the half-disc hanging under the second seam: its flat top on the seam
half = [(-1, 0.45)] + [(math.cos(a), 0.45 + 1.1 * math.sin(a)) for a in np.linspace(math.pi, 2 * math.pi, 15)[1:-1]] \
       + [(1, 0.45)]
HD = 0.03
dh = unit(0, -1, (SEAM2 - 0.45 * HD - C[2]) / R[1])
k.decal(on_box(dh), (0, -1, 0), (HD, HD), CS, eps=0.0045, outline=polygon(half), rings=12, segs=72)
# the frown: a short arc curving down
CF = srgb(INK)


def frown(u, v):
    c = np.tile(CO, (len(u), 1))
    c[(np.abs(v - (-0.55 + 0.9 * (1 - u * u))) < 0.32) & (np.abs(u) < 0.85)] = CF
    return c


df = unit(0, -1, (0.372 - C[2]) / R[1])
k.decal(on_box(df), (0, -1, 0), (0.016, 0.01), frown, eps=0.004, rings=12, segs=56)

k.build(voxel=0.005)

# Eyes: white with a pale violet iris, a dark pupil, a glint, a thin dark edge (rotom.py's)
CW, CI, CP, CK = srgb(WHITE), srgb(IRIS), srgb(PUPIL), srgb(INK)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[((u - 0.04) / 0.56) ** 2 + ((v + 0.04) / 0.64) ** 2 < 1] = CI
    c[((u - 0.04) / 0.22) ** 2 + ((v + 0.04) / 0.3) ** 2 < 1] = CP
    c[((u + 0.14) / 0.17) ** 2 + ((v - 0.26) / 0.17) ** 2 < 1] = CW
    c[np.sqrt(u * u + v * v) > 0.84] = CK
    return c


de = unit(0.042 / R[1], -1, (0.405 - C[2]) / R[1])
E = on_box(de)


def eyes(state):
    k.decal(E, (0.06, -1, 0), (0.025, 0.028), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20,
            segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
