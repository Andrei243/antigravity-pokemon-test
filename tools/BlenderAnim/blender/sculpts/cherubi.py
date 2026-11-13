# Cherubi sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cherubi.py -- <the export's Cherubi.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="leaf")

PINK, STRIPE, FEET, GREEN, STEM = "#E8607A", "#9A1E4A", "#D04E68", "#3DA84A", "#4E9A3E"
INK, PLUM, CRIMSON, MOUTH, TONGUE = "#5A1230", "#6E1636", "#B02A50", "#6A1430", "#E8788C"
C = np.array([0, 0, 0.19])                # the big cherry's middle
R = np.array([0.175, 0.165, 0.165])


# The big cherry: round, a little flattened underneath, on two small stubs of feet
def squat(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.88, V[:, 2])
    s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("cherry", ell(C, R, deform=squat), PINK, "body")
k.add("foot", ell((0.08, -0.035, 0.034), (0.038, 0.044, 0.034)), FEET, "body", mirror=True)

# The dark line from the top down its front to the eyes' height, a little to one side and curving
TH = np.linspace(0.12, 1.3, 60)
LINE = np.stack([-0.012 + 0.026 * (TH - 0.12) / 1.18, -R[1] * np.sin(TH), R[2] * np.cos(TH)], axis=1) + C
def stripe(P):
    d = np.min(np.linalg.norm(P[:, None, :] - LINE[None], axis=2), axis=1)
    return inside(d - 0.011, 0.004) * (P[:, 2] > 0.18)
k.paint(stripe, STRIPE)

# The small cherry hanging on its left (-X), on the stem's bone
SC = np.array([-0.255, 0.02, 0.29])
SR = 0.076
k.add("small", ell(SC, (SR, SR * 0.97, SR)), PINK, "stem")

# The stem: out of the top, up to where the leaves part, and down again to the small cherry (loose, crisp)
J = np.array([-0.085, 0.02, 0.45])
k.add("stem", tube([(0, 0.01, 0.33), (-0.02, 0.014, 0.4), J], [0.014, 0.013, 0.012]), STEM, "stem", fuse=False,
      surface="leaf")
k.add("stem2", tube([J, (-0.17, 0.02, 0.445), (-0.235, 0.02, 0.4), (SC[0], SC[1], SC[2] + SR - 0.01)],
                    [0.012, 0.011, 0.01, 0.01]), STEM, "stem", fuse=False, surface="leaf")


def curved_leaf(base, tip, width, arch, facing, droop=0.25, thick=0.012):
    """A broad leaf from base to a point, arched along its length and its edges let down, its face to facing."""
    base, tip = np.array(base, float), np.array(tip, float)
    L = np.linalg.norm(tip - base)
    d = (tip - base) / L
    n = np.array(facing, float); n = n - d * np.dot(n, d); n /= np.linalg.norm(n)
    s = np.cross(d, n)
    def at(u, v):
        u = 0.015 + 0.97 * u            # never a width of nought, or the sheet's normals there are nan
        w = width * 1.75 * np.sqrt(u) * (1 - u) ** 0.85
        x = (2 * v - 1)
        lift = arch * np.sin(math.pi * u) - droop * width * x * x * np.sin(math.pi * u)
        return (base[None, None] + d[None, None] * (L * u)[..., None] + s[None, None] * (x * w)[..., None]
                + n[None, None] * lift[..., None])
    return sheet(at, 28, 13, thick)

# Two green leaves where the stem parts: a big one up and over to its right, a smaller one out over the small cherry
k.add("leafbig", curved_leaf(J + (0.005, 0, 0.005), (0.2, 0.0, 0.5), 0.08, 0.075, (0, -0.55, 0.85)), GREEN, "stem",
      fuse=False, surface="leaf")
k.add("leafsmall", curved_leaf(J + (-0.01, 0, 0.005), (-0.31, 0.03, 0.43), 0.06, 0.06, (0, -0.5, 0.85)), GREEN,
      "stem", fuse=False, surface="leaf")

# The small cherry's face, turned out to the side: an eye shut in a happy curve, an open smile
CP, CI = srgb(PINK), srgb(INK)
def shut(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(v - (-0.3 + 0.75 * (1 - u * u))) < 0.24) & (np.abs(u) < 0.85)] = CI
    return c
fe = unit(-0.42, -0.8, 0.42)
k.decal(SC + fe * SR, fe, (0.02, 0.016), shut, up=unit(0, 0, 1), eps=0.003, rings=8, segs=36)
CM, CT = srgb(MOUTH), srgb(TONGUE)
def open_mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.6) ** 2 + ((v + 0.7) / 0.5) ** 2 < 1] = CT
    return c
def mouth_shape(th):
    s_ = math.sin(th)
    return min(1.0, 0.3 / s_) if s_ > 0 else 1.0
fm = unit(-0.52, -0.8, -0.26)
k.decal(SC + fm * SR, fm, (0.017, 0.016), open_mouth, eps=0.003, outline=mouth_shape, rings=10, segs=40)

# The big cherry's smile
def smile(u, v):
    c = np.tile(CP, (len(u), 1))
    c[(np.abs(v - (-0.3 + 0.8 * u * u)) < 0.2) & (np.abs(u) < 0.9)] = CI
    return c
fs = unit(0, -1, -0.15)
k.decal(C + fs * 0.165, fs, (0.04, 0.018), smile, eps=0.003, rings=8, segs=40)

# Small eyes, dark plum with a crimson lower half and a glint, set well apart at the middle's height
CPL, CCR = srgb(PLUM), srgb(CRIMSON)
def eye(u, v):
    c = np.tile(CPL, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.25) / 0.55) ** 2 < 1] = CCR
    c[((u - 0.2) / 0.34) ** 2 + ((v - 0.4) / 0.3) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.5, -0.86, 0.02)
E = C + de * 0.17
def eyes(state):
    k.decal(E, de, (0.019, 0.026), eye_look(state, eye, CP, CPL), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
