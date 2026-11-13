# Silcoon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/silcoon.py -- <the export's Silcoon.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")
# The kit keeps each vertex's four strongest bones and this skeleton has two: two empty groups make up the four

SILK, THREAD, SOCKET = "#EEF0F7", "#9C9FB4", "#C3C9E6"
C = np.array([0.0, 0.0, 0.31])               # the cocoon's middle, a little over the body's joint
R = np.array([0.27, 0.215, 0.195])           # wider than it is deep or tall

# The silk's wrappings: grooves running from the top to the bottom like the lines of a melon, unevenly spaced, the
# silk swelling a little between them
LINES = np.radians([-158, -118, -80, -44, -10, 26, 62, 100, 138])
def wrapped(V):
    phi = np.arctan2(V[:, 0], -V[:, 1])
    rho = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
    dip = np.zeros(len(V))
    for a in LINES:
        d = np.angle(np.exp(1j * (phi - a)))
        dip = np.maximum(dip, np.exp(-(d / 0.075) ** 2))
    return V * (1 - 0.02 * dip * np.clip(rho * 1.6, 0, 1))[:, None]
k.add("cocoon", ell(C, R, deform=wrapped, sub=5), SILK, "body")

def on_shell(d):
    """The point of the cocoon's surface along a direction from its middle, and the surface's normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = C + d / math.sqrt(((d / R) ** 2).sum())
    n = (p - C) / R ** 2
    return p, n / np.linalg.norm(n)

# The thread lines in the grooves
def threads(P):
    q = (P - C) / R
    phi = np.arctan2(q[:, 0], -q[:, 1])
    rho = np.sqrt((P[:, 0] - C[0]) ** 2 + (P[:, 1] - C[1]) ** 2)
    m = np.zeros(len(P))
    for a in LINES:
        d = np.abs(np.angle(np.exp(1j * (phi - a)))) * rho
        m = np.maximum(m, inside(d - 0.0036, 0.0022))
    return m * smoothstep(0.03, 0.07, rho)
k.paint(threads, THREAD)

# Spikes of hardened silk standing off the cocoon all round: a fused flare at the foot of each so it grows out of the
# silk, and the spike itself a loose piece that keeps its point
SPIKES = [((0.06, 0.05, 1.0), 0.17), ((-0.9, -0.12, 0.5), 0.17), ((0.62, -0.15, 0.78), 0.15), ((1.0, 0.05, -0.08), 0.18),
          ((0.5, -0.3, -0.8), 0.13), ((-0.12, -0.4, -1.0), 0.1), ((-0.78, -0.15, -0.6), 0.14), ((-0.25, -0.55, 0.8), 0.12),
          ((0.3, 0.9, 0.45), 0.15), ((-0.45, 0.85, -0.25), 0.15), ((-0.35, 0.55, 0.85), 0.13), ((0.55, 0.75, -0.4), 0.13)]
for i, (d, length) in enumerate(SPIKES):
    p, n = on_shell(d)
    a = unit(*d) * 0.35 + n * 0.65; a /= np.linalg.norm(a)
    k.add(f"flare{i}", tube([p - a * 0.03, p + a * 0.02], [0.05, 0.026]), SILK, "body")
    k.add(f"spike{i}", tube([p, p + a * length * 0.45, p + a * length], [0.03, 0.015, 0.0012]), SILK, "body",
          fuse=False, surface="shell")

# The eyes, one on each side of the front, looking out of a gap in the wrapping: a red iris with a black slit, the
# pale blue-grey socket round it and below, and a heavy lid of silk ragged at its edge
IRIS, IRISRIM, PUPIL, LIDC, INK, SOCK = (srgb(h) for h in ("#DE4655", "#9E2232", "#151018", SILK, "#5A5C70", SOCKET))
def eye(u, v):
    c = np.tile(SOCK, (len(u), 1))
    w = ((u + 0.05) / 0.6) ** 2 + ((v + 0.08) / 0.68) ** 2
    c[w < 1] = IRISRIM
    c[w < 0.8] = IRIS
    c[(np.abs(u + 0.05) < 0.12) & (np.abs(v + 0.08) < 0.42)] = PUPIL
    c[((u + 0.25) ** 2 + (v - 0.2) ** 2) < 0.012] = srgb("#FFFFFF")
    edge = 0.34 + 0.13 * tri(u * 1.4 + 0.25) - 0.1 * u
    c[v > edge] = LIDC
    c[np.abs(v - edge) < 0.07] = INK
    c[u * u + v * v > 0.9] = INK
    return c
PHI = math.radians(34)
EP, EN = on_shell((math.sin(PHI), -math.cos(PHI), -0.08))
def eyes(state):
    k.decal(EP, EN, (0.05, 0.056), eye_look(state, eye, LIDC, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006, smooth=3)
k.eye_states(eyes)
k.export(out)
