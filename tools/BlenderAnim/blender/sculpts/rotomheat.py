# Rotom-Heat sculpted from nothing on the skeleton and clips of the game's own export of the form (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rotomheat.py -- <the export's Rotom-Heat.glb> <out.glb>
#
# Grown from rotom.py: the same orange, the same opaque aura set a little BEHIND the core so it shows as a rim and
# leaves the face free, the same core copied behind so it shows through the aura from the back, the same spike.
# The design, from the references: Rotom inside a microwave oven, a rounded orange box about as wide as it is tall
# with a domed top; a black frame with a grey glass door across the lower front, wrapping round onto its right side
# (+X); two eyes above it, white with a red ring and a pink middle, and a small round mouth between and under them;
# the spike on top leaning to its right (-X); a red aura round it all; two thin red bolts hanging from its sides,
# each ending in a big red oven-mitt claw with white flecks.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, args, boxy, slab, outward

ORANGE, AURA, FLECK = "#F27028", "#E2545E", "#F6EEF0"
FRAME, GLASS, GLINT = "#24222A", "#8E949E", "#C4C9D0"
WHITE, RING, PINK, INK, MOUTH, MOUTHIN = "#FFFFFF", "#E0384A", "#F4848E", "#3A1A22", "#7A2028", "#F28890"

C = np.array([0.0, 0.0, 0.41])            # the oven's middle
R = np.array([0.158, 0.13, 0.142])        # its half sizes
N = 3.4                                   # how square it is
BACK = 0.02                               # the plane the core is mirrored about for its copy behind
SLAB_Y, SLAB_R = 0.105, 0.05              # the aura's slab: its middle behind the oven's, and its half depth


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


src, out = args()
k = Kit(src, coat="shell")

# --- the orange core: the oven, a dome on its top, the spike (fused, for the face's patches) ------------------
k.add("oven", box(), ORANGE, "body")
k.add("dome", ell(C + np.array([0, 0.004, 0.05]), (0.15, 0.124, 0.115), sub=5), ORANGE, "body")
SPB = C + np.array([-0.012, 0.01, 0.13])
SPT = C + np.array([-0.05, 0.02, 0.27])
SPM = (SPB + SPT) / 2 + np.array([0.004, 0, 0])
k.add("spike", tube([SPB, SPM, SPT], [0.05, 0.03, 0.007]), ORANGE, "body")

# --- the aura: opaque red a little larger than the core and set back --------------------------------------------
# A box's aura is a thin rounded slab behind its back half (a full shell larger all round reddened its sides)
AB = np.array([0, 0.022, 0])
AS = np.array([0, SLAB_Y, 0])
k.add("auraoven", box(C + AS + np.array([0, 0, -0.004]), (R[0] * 1.13, SLAB_R, R[2] * 1.13)), AURA, "body",
      fuse=False, surface="glow")
k.add("auradome", ell(C + AS + np.array([0, 0, 0.05]), (0.17, SLAB_R, 0.135), sub=5), AURA, "body",
      fuse=False, surface="glow")
ext = SPT + (SPT - SPB) / np.linalg.norm(SPT - SPB) * 0.022
k.add("auraspike", tube([SPB + AB * 0.9, SPM + AB * 0.8, SPT + AB * 0.6, ext + AB * 0.4],
                        [0.06, 0.04, 0.016, 0.003]), AURA, "body", fuse=False, surface="glow")


# From behind, the oven shows through the aura as it does from the front: the core mirrored front to back
def behind(shape):
    V, F = shape
    V = V.copy(); V[:, 1] = 2 * BACK - V[:, 1]
    return outward((V, F))


k.add("backoven", behind(box()), ORANGE, "body", fuse=False)
k.add("backdome", behind(ell(C + np.array([0, 0.004, 0.05]), (0.15, 0.124, 0.115), sub=5)), ORANGE, "body", fuse=False)

# --- the arms: thin red bolts hanging from the sides, each ending in a mitt with white flecks -----------------
AX, AZ = np.array([1.0, 0, 0]), np.array([0, 0, 1.0])
for s, side in ((1, "R"), (-1, "L")):
    # the bolt, a zigzag in the front's plane from inside the oven's side down and out
    line = [(0.13, 0.345), (0.2, 0.31), (0.178, 0.235), (0.226, 0.18), (0.215, 0.13)]
    line = [(a * s, b) for a, b in line]
    O = np.array([0.0, -0.02, 0.0])
    k.add("bolt" + side, slab(offset_outline(line, [0.016, 0.016, 0.016, 0.016, 0.02]), O, AX, AZ, 0.022),
          AURA, "arm" + side, fuse=False, surface="glow")
    # the mitt: a flat thick claw facing the front, its fingers down and a thumb to the inside
    mitt = [(-0.03, 0.03), (0.035, 0.03), (0.06, -0.035), (0.055, -0.075), (0.02, -0.095), (-0.012, -0.085),
            (-0.02, -0.05), (-0.045, -0.06), (-0.068, -0.045), (-0.055, -0.01)]
    mitt = [(1.2 * a * s, 1.2 * b) for a, b in mitt]
    M = np.array([0.228 * s, -0.022, 0.12])
    ex = np.array([math.cos(math.radians(18)), -math.sin(math.radians(18)) * s, 0])  # turned a little outward
    k.add("mitt" + side, slab(mitt, M, ex, AZ, 0.036), AURA, "arm" + side, fuse=False, surface="glow")
    # white flecks, thicker than the mitt so they show on both faces
    for fl in ([(0.005, -0.005), (0.04, -0.04), (0.032, -0.05), (-0.002, -0.015)],
               [(0.02, -0.05), (0.045, -0.072), (0.036, -0.08), (0.012, -0.06)]):
        fl = [(1.2 * a * s, 1.2 * b) for a, b in fl]
        k.add("fleck" + side, slab(fl, M, ex, AZ, 0.042), FLECK, "arm" + side, fuse=False, surface="glow")

# --- the window: a black frame across the lower front and round onto its right side, grey glass inside -------
CF = srgb(FRAME)
FY = C[1] - R[1]           # the front


def frame(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    band = inside(z - 0.395, 0.0025) * inside(0.302 - z, 0.0025)
    front = inside(y - (FY + 0.05), 0.0025) * inside(np.abs(x) - 0.136, 0.0025)
    side = inside(-x + 0.06, 0.0025) * inside(y - 0.07, 0.0025)       # wrapping round onto +X (its left)
    return band * np.maximum(front, side)


k.paint(frame, FRAME)
CG, CL = srgb(GLASS), srgb(GLINT)


def glass(u, v):
    c = np.tile(CG, (len(u), 1))
    c[(np.abs(v - 0.55) < 0.12) & (np.abs(u) < 0.8)] = CL
    return c


rr = lambda th: 1 / max(abs(math.cos(th)) ** 6 + abs(math.sin(th)) ** 6, 1e-6) ** (1 / 6)
k.decal(on_box((0, -1, -0.48)), (0, -1, 0), (0.108, 0.036), glass, eps=0.004,
        outline=rr, rings=14, segs=72)

# --- the face: a small round mouth under and between the eyes ----------------------------------------------------
CO, CM, CMI = srgb(ORANGE), srgb(MOUTH), srgb(MOUTHIN)


def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[u * u + (v + 0.15) ** 2 < 0.3] = CMI
    return c


dm = unit(0, -1, 0.04)
k.decal(on_box(dm), dm, (0.013, 0.015), mouth, eps=0.004, rings=12, segs=48)

k.build(voxel=0.005)

# Eyes: white with a red ring and a pink middle, a glint, a dark edge
CW, CR, CP, CK = srgb(WHITE), srgb(RING), srgb(PINK), srgb(INK)


def eye(u, v):
    r = np.sqrt(u * u + v * v)
    c = np.tile(CW, (len(u), 1))
    c[r < 0.7] = CR
    c[r < 0.48] = CP
    c[((u + 0.18) / 0.17) ** 2 + ((v - 0.2) / 0.17) ** 2 < 1] = CW
    c[r > 0.86] = CK
    return c


de = unit(0.34, -1, 0.4)
E = on_box(de)


def eyes(state):
    k.decal(E, (0.05, -1, 0.06), (0.03, 0.033), eye_look(state, eye, CO, CK, line=0.16), eps=0.004, rings=20,
            segs=56, mirror=True)


k.eye_states(eyes)
k.export(out)
