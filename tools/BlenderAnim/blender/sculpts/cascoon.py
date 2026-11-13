# Cascoon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cascoon.py -- <the export's Cascoon.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")
# The kit keeps each vertex's four strongest bones and this skeleton has two: two empty groups make up the four

SILK, THREAD, SOCKET = "#DCC7E4", "#8E7A9E", "#8C5FA0"
C = np.array([0.0, 0.0, 0.31])               # the cocoon's middle, a little over the body's joint
R = np.array([0.25, 0.225, 0.205])           # rounder than Silcoon's

# The silk's wrappings: grooves from top to bottom, unevenly spaced, the silk swelling a little between them
LINES = np.radians([-150, -105, -64, -28, 12, 50, 88, 128, 170])
def wrapped(V):
    phi = np.arctan2(V[:, 0], -V[:, 1])
    rho = np.sqrt(V[:, 0] ** 2 + V[:, 1] ** 2)
    dip = np.zeros(len(V))
    for a in LINES:
        d = np.angle(np.exp(1j * (phi - a)))
        dip = np.maximum(dip, np.exp(-(d / 0.075) ** 2))
    return V * (1 - 0.018 * dip * np.clip(rho * 1.6, 0, 1))[:, None]
k.add("cocoon", ell(C, R, deform=wrapped, sub=5), SILK, "body")

def on_shell(d):
    """The point of the cocoon's surface along a direction from its middle, and the surface's normal there."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    p = C + d / math.sqrt(((d / R) ** 2).sum())
    n = (p - C) / R ** 2
    return p, n / np.linalg.norm(n)

def threads(P):
    q = (P - C) / R
    phi = np.arctan2(q[:, 0], -q[:, 1])
    rho = np.sqrt((P[:, 0] - C[0]) ** 2 + (P[:, 1] - C[1]) ** 2)
    m = np.zeros(len(P))
    for a in LINES:
        d = np.abs(np.angle(np.exp(1j * (phi - a)))) * rho
        m = np.maximum(m, inside(d - 0.0034, 0.0022))
    return m * smoothstep(0.03, 0.07, rho)
k.paint(threads, THREAD)

# Short spikes of hardened silk all round, each grown out of a fused flare and ending in a loose point
SPIKES = [((0.15, 0.1, 1.0), 0.12), ((0.6, -0.1, 0.8), 0.15), ((-0.95, -0.05, 0.4), 0.17), ((1.0, 0.0, -0.15), 0.14),
          ((0.45, -0.35, -0.85), 0.12), ((-0.25, -0.3, -1.0), 0.1), ((-0.8, -0.2, -0.6), 0.13), ((-0.3, -0.5, 0.85), 0.11),
          ((0.35, 0.85, 0.45), 0.14), ((-0.5, 0.8, -0.2), 0.14), ((-0.4, 0.5, 0.85), 0.12), ((0.6, 0.7, -0.45), 0.12)]
for i, (d, length) in enumerate(SPIKES):
    p, n = on_shell(d)
    a = unit(*d) * 0.35 + n * 0.65; a /= np.linalg.norm(a)
    k.add(f"flare{i}", tube([p - a * 0.03, p + a * 0.02], [0.05, 0.026]), SILK, "body")
    k.add(f"spike{i}", tube([p, p + a * length * 0.45, p + a * length], [0.03, 0.015, 0.0012]), SILK, "body",
          fuse=False, surface="shell")

# The eyes, one on each side: half-shut, a red eye with a pale upright slit set in a deep purple socket, the lilac lid
# over the top of it
IRIS, IRISRIM, SLIT, LIDC, INK, SOCK = (srgb(h) for h in ("#E04656", "#A22A3C", "#F4EEF2", SILK, "#5E4870", SOCKET))
def eye(u, v):
    c = np.tile(SOCK, (len(u), 1))
    w = ((u + 0.08) / 0.66) ** 2 + ((v + 0.1) / 0.55) ** 2
    c[w < 1] = IRISRIM
    c[w < 0.78] = IRIS
    c[(np.abs(u + 0.08) < 0.09) & (np.abs(v + 0.1) < 0.3)] = SLIT
    edge = 0.3 - 0.12 * u
    c[v > edge] = LIDC
    c[np.abs(v - edge) < 0.07] = INK
    c[u * u + v * v > 0.9] = INK
    return c
PHI = math.radians(38)
EP, EN = on_shell((math.sin(PHI), -math.cos(PHI), -0.05))
def eyes(state):
    k.decal(EP, EN, (0.06, 0.05), eye_look(state, eye, LIDC, INK), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006, smooth=3)
k.eye_states(eyes)
k.export(out)
