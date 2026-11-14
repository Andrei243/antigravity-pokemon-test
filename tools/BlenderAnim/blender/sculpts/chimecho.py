# Chimecho sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chimecho.py -- <the export's Chimecho.glb> <out.glb>
#
# Grown from Chingling's script: the same shell coat, the same small eyes laid as patches and loose pieces for what
# must keep its edges (Chingling's cords, here the tail).
# From the references: a wind chime. A round pale blue head, wider than tall and a little flat underneath, hung from a
# yellow sucker of a disc on a short yellow stalk on its top, the disc's face tilted forward; two small round yellow
# eyes in a dark rim set close above the middle of the face, a long thin shallow smile under them; blocky red marks on
# its +X side, a large one over the upper side and a small one low on the -X side; a small round nub of an arm out at
# each lower side; underneath an open hollow, the bell's mouth, from which a thin stalk hangs and widens into a long
# flat ribbon of a tail, wider and flared at its slanted bottom, whose lower part is red with a blocky ragged edge.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="shell")

HEAD, RED, YELLOW, YDARK, INK, HOLLOW = "#B9DDEF", "#DD4458", "#F2CE4E", "#D9AE3A", "#2C2A36", "#86AFCB"
H = np.array([0.0, 0.0, 0.652])
R = np.array([0.125, 0.115, 0.092])
E_ = 0.003


def flatbottom(V):
    V = V.copy()
    V[:, 2] = np.where(V[:, 2] < 0, V[:, 2] * 0.8, V[:, 2])
    return V


# --- the head, its nubs of arms, the hollow underneath -------------------------------------------------------
k.add("head", ell(H, R, deform=flatbottom), HEAD, "body")
k.add("nub", ell((0.122, -0.035, 0.6), (0.036, 0.026, 0.024), rot=(0, 34, 0)), HEAD, "arm{s}", mirror=True)
k.add("mouth", ell((0, 0, 0.583), (0.07, 0.064, 0.026)), HOLLOW, "body", carve=True)

# --- the sucker on top: a short stalk and a yellow disc tilted forward ----------------------------------------
k.add("stalk", tube([(0, 0.0, 0.72), (0, 0.004, 0.77), (0, 0.008, 0.8)], [0.012, 0.011, 0.011]), YELLOW, "body",
      fuse=False, surface="shell")
k.add("cup", ell((0, 0.008, 0.808), (0.07, 0.062, 0.02), rot=(40, 0, 0)), YELLOW, "body", fuse=False, surface="shell")
k.add("cupunder", ell((0, 0.016, 0.8), (0.058, 0.05, 0.014), rot=(40, 0, 0)), YDARK, "body", fuse=False,
      surface="shell")

# --- the red marks: blocky patches round the head (angle round its upright axis, height) -----------------------
MARKS = [(0.55, 0.95, 0.69, 0.728), (0.8, 1.35, 0.664, 0.732), (1.2, 1.62, 0.638, 0.7), (1.0, 1.22, 0.645, 0.672),
         (1.5, 1.85, 0.66, 0.69), (-1.45, -1.15, 0.638, 0.672), (-1.32, -1.08, 0.66, 0.69)]
def marks(P):
    q = P - H
    a = np.arctan2(q[:, 0], -q[:, 1])
    m = np.zeros(len(P))
    for a0, a1, z0, z1 in MARKS:
        m = np.maximum(m, inside(a0 - a, 0.008) * inside(a - a1, 0.008) * inside(z0 - P[:, 2], E_) * inside(P[:, 2] - z1, E_))
    return m * (np.linalg.norm(q, axis=1) < 0.16)
k.paint(marks, RED, on=["head"])

# --- the stalk under the head and the ribbon of a tail ---------------------------------------------------------
k.add("neck", blade([(0, 0.0, 0.615), (0, 0.0, 0.55), (0, 0.0, 0.49), (0, 0.0, 0.458)],
                    [0.012, 0.011, 0.014, 0.034], (0, 1, 0), 0.55), HEAD, "tail", fuse=False, surface="shell")

TOP, LEN = 0.462, 0.4
def ribbon(start, thick_out=0.0):
    """The tail's middle surface; start(u) is where along it a piece begins (0 the top)."""
    def at(u, v):
        v0 = start(u)
        v = v0 + (1 - v0) * (0.015 + (0.97 + thick_out) * v)
        c = 2 * u - 1
        w = (0.056 + 0.08 * v ** 2) * (0.6 + 0.4 * np.clip(v / 0.06, 0, 1) ** 0.5) * (1 + thick_out)
        x = c * w + 0.025 * v ** 2
        z = TOP - LEN * v - 0.022 * v ** 4 * c
        y = 0.018 * np.sin(np.pi * v) - 0.025 * v ** 2
        return np.stack([x, y, z], -1)
    return at
k.add("tail", sheet(ribbon(lambda u: 0 * u), 22, 64, 0.022), HEAD, "tail", fuse=False, surface="shell")
RAG = np.array([0.69, 0.63, 0.67, 0.57, 0.6, 0.49, 0.53])      # the red's blocky top edge, -X to +X
def ragged(u):
    return RAG[np.clip((u * len(RAG)).astype(int), 0, len(RAG) - 1)]
# a little wider and longer than the blue, so their edges never share a plane
k.add("tailred", sheet(ribbon(ragged, 0.025), 71, 36, 0.03), RED, "tail", fuse=False, surface="shell")

# --- the face: small round yellow eyes in a dark rim, close above the middle; a long shallow smile -------------
CY, CI, CH = srgb("#F2D24A"), srgb(INK), srgb(HEAD)
def eye(u, v):
    c = np.tile(CY, (len(u), 1))
    c[u * u + v * v > 0.6] = CI
    c[((u - 0.2) / 0.2) ** 2 + ((v - 0.25) / 0.2) ** 2 < 1] = srgb("#FFF4C0")
    return c
def smile(u, v):
    c = np.tile(CH, (len(u), 1))
    line = -0.15 + 0.35 * u * u + 0.1 * u
    c[(np.abs(v - line) < 0.16) & (np.abs(u) < 0.92)] = CI
    return c
md = unit(0.03, -1, -0.12)
k.decal(H + md * R, md, (0.07, 0.022), smile, eps=0.004, rings=10, segs=48)

def eyes(state):
    for s in (1, -1):
        dn = unit(s * 0.42, -0.86, 0.35)
        e = H + dn * R
        look = eye_look(state, eye, CH, CI, line=0.18)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, dn, (0.018, 0.018), lk, eps=0.003, rings=20, segs=56)

k.build(voxel=0.004, smooth=3)
k.eye_states(eyes)
k.export(out)
