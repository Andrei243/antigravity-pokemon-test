# Mantyke sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mantyke.py -- <the export's Mantyke.glb> <out.glb>
#
# From the references: a small round manta. A domed body, blue on top and pale blue beneath, the pale face on its
# front up to a little over the eyes; two round flipper-wings out to the sides, their tips curling up and back, blue on
# top and pale beneath; two antennae rising from the head and arching back, each ending in a rounded paddle; big round
# black eyes with a white glint and two short lashes at the outer lower corner; an open smiling mouth, pink with a dark
# rim; a short curved line on each lower side of the body; on its back two red spots and a curved line, a second face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, PALE, RED, INK, MOUTH, TONGUE = "#3E8EDC", "#B4DAF2", "#E4483C", "#16202E", "#D8586A", "#F08C98"

# ---- the body: a dome, a little longer than wide, the face a rounded front on it
k.add("body", ell((0, 0.01, 0.125), (0.13, 0.15, 0.092)), BLUE, "body")
k.add("face", ell((0, -0.07, 0.122), (0.112, 0.085, 0.085)), BLUE, "head")

# ---- the wings: round flippers from the body's sides, their tips curling up and back
WC, WR = np.array([0.195, 0.025, 0.128]), np.array([0.11, 0.1, 0.03])
RISE, SWEEP = 2.0, 0.7
def bend(V):
    V = V.copy()
    t = np.clip((V[:, 0] + 0.6) / 1.6, 0, 1)
    V[:, 2] += RISE * t * t
    V[:, 1] += SWEEP * t * t
    return V
k.add("wing", ell(WC, WR, deform=bend), BLUE, "fin{s}", mirror=True)

def wing_mid(ax):
    t = np.clip(((ax - WC[0]) / WR[0] + 0.6) / 1.6, 0, 1)
    return WC[2] + RISE * WR[2] * t * t

# Pale beneath the line that runs along the wings' middle and over the face, a little above the eyes
def pale(P):
    ax, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
    face = smoothstep(-0.02, -0.11, y) * (1 - smoothstep(0.09, 0.15, ax))
    line = (wing_mid(ax) - 0.006) * (1 - face) + 0.178 * face
    return inside(z - line, 0.003)
k.paint(pale, PALE)

# ---- the back's second face: two red spots and a curved line between and behind them
def back_mark(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    top = inside(0.185 - z, 0.004)
    arc = np.abs(y - (0.105 - 11.0 * x * x)) - 0.006
    return top * inside(arc, 0.003) * inside(np.abs(x) - 0.06, 0.004)
k.paint(back_mark, INK)
def back_spots(P):
    m = np.zeros(len(P))
    for s in (1, -1):
        d = np.hypot(P[:, 0] - 0.05 * s, P[:, 1] - 0.035)
        m = np.maximum(m, inside(d - 0.017, 0.003))
    return m * inside(0.18 - P[:, 2], 0.004)
k.paint(back_spots, RED, on=["body"])

# ---- the antennae: from the top of the head, rising and arching back, rounded paddles at their ends
ANT = [(0.035, -0.045, 0.19), (0.048, -0.045, 0.25), (0.06, -0.025, 0.305), (0.07, 0.01, 0.335)]
k.add("antenna", tube(ANT, [0.015, 0.014, 0.013, 0.012]), BLUE, "head", mirror=True)
k.add("paddle", ell((0.073, 0.03, 0.34), (0.024, 0.036, 0.02), rot=(-20, 0, 0)), BLUE, "head", mirror=True)

# ---- the tail: short and thin
k.add("tail", tube([(0, 0.13, 0.115), (0, 0.18, 0.112), (0, 0.215, 0.115)], [0.014, 0.01, 0.005]), BLUE, "tail")

# ---- the mouth: an open smile under the eyes, a dark rim, pink inside and a lighter tongue
def m_top(u):
    return 0.55 - 0.12 * u * u
def m_bot(u):
    return -0.95 + 0.8 * u * u
MW = [(u, m_top(u)) for u in np.linspace(-1, 1, 25)] + [(u, m_bot(u)) for u in np.linspace(1, -1, 25)[1:-1]]
CM, CT, CK = srgb(MOUTH), srgb(TONGUE), srgb(INK)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[v < m_bot(u) + 0.6 - 0.3 * u * u] = CT
    c[(v > m_top(u) - 0.2) | (v < m_bot(u) + 0.2) | (np.abs(u) > 0.88)] = CK
    return c
k.decal((0, -0.16, 0.108), unit(0, -1, -0.1), (0.03, 0.022), mouth, eps=0.004, outline=polygon(MW), rings=20,
        segs=72)

# ---- a short curved line on each lower side, under the wing's root
def side_line(u, v):
    c = np.tile(srgb(PALE), (len(u), 1))
    c[np.abs(v - (-0.35 + 0.75 * u * u)) < 0.22] = CK
    return c
k.decal((0.1, -0.1, 0.085), unit(0.6, -0.7, -0.35), (0.03, 0.016), side_line, eps=0.004, rings=12, segs=40,
        mirror=True, outline=lambda th: 0.999)

k.build(voxel=0.0048, smooth=3)

# ---- the eyes: big and round, black with a white glint toward the top inner side, two short lashes at the outer
# lower corner. u is outward on both eyes.
CP, CW = srgb(PALE), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    c = np.tile(CP, (len(u), 1))
    r = np.sqrt(u * u + v * v)
    c[r < 0.78] = CK
    c[((u + 0.22) ** 2 + (v - 0.24) ** 2) < 0.26 ** 2] = CW
    for a in (-0.45, -0.85):                       # two lashes at the outer lower corner
        d = np.array([math.cos(a), math.sin(a)])
        along = u * d[0] + v * d[1]
        across = np.abs(-u * d[1] + v * d[0])
        c[(along > 0.7) & (along < 0.98) & (across < 0.07)] = CK
    return c
def eyes(state):
    dn = unit(0.32, -0.93, 0.15)
    k.decal((0.058, -0.145, 0.148), dn, (0.036, 0.038), eye_look(state, eye, CP, CK, line=0.16), eps=0.005,
            rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
