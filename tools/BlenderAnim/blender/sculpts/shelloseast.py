# Shellos (the East Sea's, blue) sculpted from nothing on the skeleton and clips of the game's own export of it
# (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shelloseast.py -- <the export's Shellos-East.glb> <out.glb>
# Grown from shellos.py (the West Sea's): the same body, eyes, lips and line between the colours, but blue over a
# lime-green underside; in place of the pink crest two round blue lobes on the head, each with a horn swept back
# and tipped white; in place of the spikes two flat wavy fins on the back, rimmed in yellow.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, GREEN, YELLOW, HORN, INKH = "#22B2E6", "#A8D838", "#F4D24E", "#F6F8FA", "#1A2230"

# The foot: green, flat on the ground, its rim in soft scallops
def scallops(V):
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    s = 1 + 0.07 * np.cos(9 * th) * smoothstep(0.2, -0.6, V[:, 2])
    V[:, 0] *= s; V[:, 1] *= s
    V[:, 2] = np.maximum(V[:, 2], -0.75)          # a flat sole
    return V
k.add("foot_front", ell((0, -0.03, 0.062), (0.118, 0.12, 0.066), deform=scallops), GREEN, "seg2")
k.add("foot_back", ell((0, 0.15, 0.058), (0.118, 0.15, 0.062), deform=scallops), GREEN, "body")
k.add("foot_tail", ell((0, 0.265, 0.052), (0.08, 0.07, 0.048)), BLUE, "tail")
# The back: a blue mantle over the hind half
k.add("mantle", ell((0, 0.11, 0.1), (0.112, 0.165, 0.078)), BLUE, "seg1")
# The chest swelling forward, green, and the neck rising out of it to the head
k.add("chest", ell((0, -0.06, 0.145), (0.1, 0.092, 0.11)), GREEN, "seg2")
k.add("neck", tube([(0, -0.035, 0.13), (0, -0.065, 0.21), (0, -0.08, 0.28)], [0.085, 0.075, 0.08]), BLUE, "seg3")
# The head and its rounded muzzle
HC = np.array([0.0, -0.08, 0.322])
k.add("skull", ell(HC, (0.1, 0.095, 0.095)), BLUE, "head")
MC = np.array([0.0, -0.15, 0.3])
MR = np.array([0.075, 0.07, 0.06])
k.add("muzzle", ell(MC, MR), BLUE, "head")

# Two round lobes on the top of the head, side by side, and from the back of each a horn swept back and up: a blue
# root fused into the lobe and a thick white cone, loose so its edge stays crisp
k.add("lobe", ell((0.048, -0.05, 0.415), (0.056, 0.062, 0.062)), BLUE, "head", mirror=True)
k.add("horn", tube([(0.06, -0.03, 0.44), (0.085, 0.012, 0.456)], [0.034, 0.028]), BLUE, "head", mirror=True)
k.add("horntip", blade([(0.082, 0.004, 0.454), (0.097, 0.035, 0.465), (0.108, 0.07, 0.482), (0.11, 0.1, 0.505)],
                       [0.027, 0.021, 0.012, 0.003], (1, 0, 0), 0.75), HORN, "head", fuse=False, mirror=True)

# Where the blue of the back meets the green of the front: the West Sea's line, from the lips round the cheeks, down
# the side of the neck and along the body's flank to the tail, with the side the green lies on at each point (W)
EDGE = [((0.0, -0.222, 0.288), (0, -0.3, -1)), ((0.05, -0.205, 0.29), (0, -0.4, -1)), ((0.075, -0.15, 0.292), (-0.2, -0.6, -0.8)),
        ((0.088, -0.11, 0.282), (-0.3, -0.8, -0.6)), ((0.095, -0.065, 0.22), (-0.4, -1, -0.15)),
        ((0.108, -0.025, 0.155), (-0.3, -1, -0.45)), ((0.12, 0.04, 0.105), (0, -0.4, -1)), ((0.122, 0.15, 0.09), (0, 0, -1)),
        ((0.095, 0.24, 0.08), (0, 0.4, -1)), ((0.0, 0.31, 0.072), (0, 1, -0.5))]
EP = np.array([p for p, _ in EDGE]); EW = np.array([unit(*w) for _, w in EDGE])
CP = np.vstack([EP[i] + (EP[i + 1] - EP[i]) * t for i in range(len(EP) - 1) for t in np.linspace(0, 1, 40, endpoint=False)])
CW = np.vstack([EW[i] + (EW[i + 1] - EW[i]) * t for i in range(len(EW) - 1) for t in np.linspace(0, 1, 40, endpoint=False)])
CW /= np.linalg.norm(CW, axis=1, keepdims=True)
def side(P):
    """The signed distance across the line (green side positive) and the distance to it, on the mirrored model."""
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    i = np.concatenate([np.argmin(((Q[a:a + 4000, None, :] - CP[None]) ** 2).sum(2), axis=1)
                        for a in range(0, len(Q), 4000)])
    return ((Q - CP[i]) * CW[i]).sum(1), np.linalg.norm(Q - CP[i], axis=1)
k.paint(lambda P: inside(-side(P)[0], 0.0025), GREEN, on=["skull", "muzzle", "neck", "chest", "mantle", "foot"])
k.paint(lambda P: inside(side(P)[0], 0.0025), BLUE, on=["skull", "muzzle", "neck", "chest", "mantle", "foot"])
def yellow(P):
    d, r = side(P)
    return inside(np.abs(d) - 0.0075, 0.0025) * inside(r - 0.03, 0.004)
k.paint(yellow, YELLOW, on=["skull", "muzzle", "neck", "chest", "mantle", "foot"])

# The lips: a raised yellow rim round the muzzle's front, its corners turned up in a smile
lip = []
for a in np.linspace(-115, 115, 31):
    t = math.radians(a)
    z = 0.282 + 0.012 * (a / 115) ** 2
    s = math.sqrt(max(0.0, 1 - ((z - MC[2]) / MR[2]) ** 2))
    lip.append(MC + np.array([MR[0] * s * math.sin(t), -MR[1] * s * math.cos(t), z - MC[2]]) * 1.02)
k.add("lips", tube(lip, [0.0085] * len(lip), seg=12, per=3), YELLOW, "head", fuse=False)

# Two flat fins on the back like little wings, leaning well out, their free edge in round lobes: a blue plate with a
# yellow tube round its free edge (its foot is in the mantle)
def wave(th, up):
    return 1 + (0.16 * np.cos(5 * th) - 0.2 * np.exp(-((th - math.pi / 2) / 0.16) ** 2)) * smoothstep(-0.3, 0.3, up)
def fin_outline(V):
    V = V.copy()
    s = wave(np.arctan2(V[:, 2], V[:, 1]), V[:, 2])
    V[:, 1] *= s; V[:, 2] *= s
    return V
FC, TILT, FR = np.array([0.05, 0.12, 0.2]), 40, (0.11, 0.1)
t_ = math.radians(TILT)
UP = np.array([math.sin(t_), 0, math.cos(t_)])
k.add("fin", ell(FC, (0.0075, FR[0], FR[1]), rot=(0, TILT, 0), deform=fin_outline), BLUE, "seg1",
      fuse=False, mirror=True)
th = np.linspace(-0.12 * math.pi, 1.12 * math.pi, 90)
s = wave(th, np.sin(th))
rim = [FC + np.array([0, FR[0] * s[i] * math.cos(th[i]), 0]) + UP * FR[1] * s[i] * math.sin(th[i]) for i in range(len(th))]
k.add("fin_rim", tube(rim, [0.0065] * len(rim), seg=10, per=2), YELLOW, "seg1", fuse=False, mirror=True)

# Two little nostrils on the muzzle
k.decal((0.022, -0.214, 0.315), unit(0.3, -1, 0.1), (0.0055, 0.0055), "#1C5A86", eps=0.003, rings=4, segs=16,
        mirror=True)

# The eyes: the West Sea's, round and black with a shine, set in a yellow ring, a little lash at the outer corner
RING, BLACK, SHINE, LID, INK = (srgb(h) for h in (YELLOW, "#141014", "#FFFFFF", BLUE, INKH))
def eye(u, v):
    c = np.tile(RING, (len(u), 1))
    c[(u / 0.62) ** 2 + (v / 0.7) ** 2 < 1] = BLACK
    c[((u + 0.18) ** 2 + (v - 0.28) ** 2) < 0.04] = SHINE
    c[u * u + v * v > 0.86] = INK
    c[(u > 0.5) & (v > 0.62) & (np.abs(v - 0.62 - 0.8 * (u - 0.5)) < 0.08)] = INK
    return c
EC = HC + unit(0.6, -0.74, 0.22) * 0.095
def eyes(state):
    k.decal(EC, unit(0.62, -0.72, 0.2), (0.032, 0.035), eye_look(state, eye, LID, INK), eps=0.005, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.005, smooth=3)
k.eye_states(eyes)
k.export(out)
