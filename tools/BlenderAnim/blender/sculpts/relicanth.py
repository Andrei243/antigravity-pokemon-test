# Relicanth sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/relicanth.py -- <the export's Relicanth.glb> <out.glb>
#
# From the references: a deep, heavy coelacanth, a little under twice as long as it is deep. The front third is a great
# blunt bony plate of tan, the head itself: a slanted front with a jutting lower lip and a small frown over it, a low
# keel along the top and a corner hanging down behind the jaw; tired eyes that are two short dark dashes on the plate's
# sides. Behind the plate the body is grey-brown with irregular tan blotches like a jigsaw's pieces, and a small red
# hexagon on each side just behind the plate. A tan fin like a shark's on the top behind the plate; a long paddle of a
# second dorsal leaning back, a broad tail fan with a scalloped rear edge and a tan patch, a fin under the tail's root,
# and two pairs of grey paddle fins hanging under the body. The joints: body (0, 0, 0.254), tail (0, 0.2, 0.254),
# finR/L (+-0.08, -0.06, 0.194). The midline fins are kept lower than the body is deep and turned some 30 degrees to
# the model's +X, so the game's cameras see their faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, hull, slab, srgb, inside, unit, eye_look, polygon, boxy, args

src, out = args()
k = Kit(src, coat="scales")

GREY, TAN, RED, INKH = "#78706A", "#C6AF92", "#E2384C", "#2A2420"
FINGREY = "#6C655F"

# --- the frame ------------------------------------------------------------------------------------------------------
HEAD_C, HEAD_R = np.array([0.0, -0.14, 0.25]), np.array([0.1, 0.16, 0.152])
BODY_C, BODY_R = np.array([0.0, 0.03, 0.245]), np.array([0.097, 0.2, 0.142])
TAPER = 0.4


def taper(V):
    """Narrowing toward the tail (local +y)."""
    V = V.copy()
    s = 1 - TAPER * np.clip(V[:, 1], 0, 1) ** 2
    V[:, 0] *= s; V[:, 2] *= s
    return V


def body_x(Y, Z):
    """The body's half-width at a place along it and a height (its side's surface), for laying marks on it."""
    t = (Y - BODY_C[1]) / BODY_R[1]
    s = 1 - TAPER * max(t, 0) ** 2
    q = 1 - t * t - ((Z - BODY_C[2]) / (BODY_R[2] * s)) ** 2
    return BODY_R[0] * s * math.sqrt(max(q, 0.0))


def plate_edge(Z):
    """Where the bony plate ends going back (y), by height: a little further back at its top and its hanging corner."""
    z = (Z - 0.25) / 0.15
    return -0.1 + 0.018 * z * z + 0.012 * np.clip(-z - 0.4, 0, None) * 3


def yawed(deg):
    """A plane running back (a) and up (b), turned deg degrees toward +X about the upright."""
    t = math.radians(deg)
    return (math.sin(t), math.cos(t), 0.0), (0.0, 0.0, 1.0)


# --- the head: the bony plate is the head, its front blunt and slanted ------------------------------------------------
k.add("head", ell(HEAD_C, HEAD_R, deform=boxy(2.4)), TAN, "body")
# The jutting lower lip and the chin under it
k.add("lip", ell((0.0, -0.285, 0.178), (0.06, 0.04, 0.036), deform=boxy(2.6)), TAN, "body")
k.add("chin", ell((0.0, -0.225, 0.14), (0.075, 0.06, 0.05)), TAN, "body")
# The plate's corner hanging down behind the jaw
k.add("cheek", ell((0.0, -0.1, 0.145), (0.088, 0.07, 0.065), deform=boxy(2.6)), TAN, "body")
# A low keel along the top of the plate
k.add("keel", tube([(0.0, -0.27, 0.36), (0.0, -0.17, 0.4), (0.0, -0.07, 0.402)], [0.028, 0.03, 0.026]), TAN, "body")

# --- the body: deep, tapering to the tail's root ------------------------------------------------------------------------
k.add("body", ell(BODY_C, BODY_R, deform=taper), GREY, "body")
k.add("stalk", tube([(0.0, 0.15, 0.25), (0.0, 0.2, 0.252), (0.0, 0.25, 0.254)], [0.07, 0.052, 0.036]), GREY, "tail")

# The plate's crisp rear edge: tan in front of it, grey behind
k.paint(lambda P: inside(P[:, 1] - plate_edge(P[:, 2]), 0.0025), TAN)
k.paint(lambda P: inside(plate_edge(P[:, 2]) - P[:, 1], 0.0025), GREY)

# --- the blotches: tan pieces of a jigsaw, each a cluster of balls on the side's surface -------------------------------
BLOTS = [   # (y, z, size): from the art
    (0.045, 0.325, 0.034), (0.085, 0.2, 0.046), (0.185, 0.28, 0.036), (0.13, 0.33, 0.022), (0.0, 0.17, 0.024),
]
rs = np.random.RandomState(369)
BALLS = []
for y, z, r in BLOTS:
    for j in range(6):
        oy, oz = rs.uniform(-0.6, 0.6, 2) * r
        rr = r * rs.uniform(0.42, 0.62)
        yy, zz = y + oy, z + oz
        x = body_x(yy, zz)
        BALLS.append((x, yy, zz, rr))
BALLS = np.array(BALLS)


def blots(P):
    d = np.full(len(P), 9.0)
    ax = np.abs(P[:, 0])
    for x, y, z, r in BALLS:
        d = np.minimum(d, np.sqrt((ax - x) ** 2 + (P[:, 1] - y) ** 2 + (P[:, 2] - z) ** 2) - r)
    return inside(d, 0.0025) * inside(plate_edge(P[:, 2]) + 0.012 - P[:, 1], 0.003)


k.paint(blots, TAN)

# --- the fins ------------------------------------------------------------------------------------------------------
# A tan fin like a shark's on the top behind the plate, solid, leaning back
k.add("dorsal1", hull([(0.022, -0.085, 0.36), (-0.022, -0.085, 0.36), (0.022, 0.01, 0.37), (-0.022, 0.01, 0.37),
                       (0.009, -0.02, 0.42), (-0.009, -0.02, 0.42), (0.0, 0.012, 0.448)]), TAN, "body", fuse=False)

# The second dorsal: a long paddle leaning back, turned to +X, grey with a tan patch near its tip
ex, ez = yawed(30)
D2 = [(-0.02, -0.03), (0.07, -0.02), (0.13, 0.03), (0.175, 0.07), (0.19, 0.095), (0.17, 0.118), (0.13, 0.11),
      (0.08, 0.085), (0.03, 0.05), (-0.02, 0.02)]
D2_O = np.array([0.0, 0.08, 0.34])
k.add("dorsal2", slab(D2, D2_O, ex, ez, 0.016), FINGREY, "body", fuse=False)
k.add("dorsal2spot", slab([(0.12, 0.06), (0.155, 0.08), (0.17, 0.1), (0.15, 0.108), (0.125, 0.093), (0.11, 0.075)],
                          D2_O, ex, ez, 0.022), TAN, "body", fuse=False)

# The tail fan: broad, its rear edge scalloped, a tan patch high on its back edge
ex, ez = yawed(30)
FAN = [(-0.02, -0.05), (0.04, -0.075), (0.1, -0.115), (0.155, -0.12), (0.14, -0.08), (0.165, -0.05), (0.15, -0.015),
       (0.172, 0.02), (0.155, 0.055), (0.17, 0.09), (0.15, 0.12), (0.1, 0.115), (0.04, 0.075), (-0.02, 0.05)]
FAN_O = np.array([0.0, 0.225, 0.254])
k.add("tailfan", slab(FAN, FAN_O, ex, ez, 0.018), FINGREY, "tail", fuse=False)
k.add("tailspot", slab([(0.115, 0.0), (0.15, 0.01), (0.155, 0.05), (0.13, 0.07), (0.11, 0.045)], FAN_O, ex, ez, 0.024),
      TAN, "tail", fuse=False)

# A fin under the tail's root, swept back and down
ex, ez = yawed(30)
ANAL = [(-0.02, 0.03), (0.05, 0.005), (0.11, -0.04), (0.15, -0.075), (0.13, -0.098), (0.08, -0.085), (0.02, -0.05), (-0.02, -0.02)]
k.add("anal", slab(ANAL, (0.0, 0.13, 0.16), ex, ez, 0.016), FINGREY, "body", fuse=False)

# Two pairs of paddle fins hanging under the body, their faces to the front, the front pair on the fin joints
PADDLE = [(-0.015, -0.03), (0.035, -0.038), (0.07, -0.04), (0.088, -0.026), (0.094, 0.0), (0.085, 0.026),
          (0.06, 0.038), (0.025, 0.034), (-0.015, 0.026)]
# Swept back and down, their faces turned half to the side and half to the front: broad paddles from both cameras
FIN_A, FIN_B = unit(0.3, 0.62, -0.72), unit(0.7, -0.62, 0.0)
k.add("pectoral", slab(PADDLE, (0.06, -0.085, 0.155), FIN_A, FIN_B, 0.022), FINGREY, "fin{s}", fuse=False, mirror=True)
k.add("pelvic", slab([(x * 0.85, y * 0.85) for x, y in PADDLE], (0.05, 0.05, 0.13), FIN_A, FIN_B, 0.022), FINGREY,
      "body", fuse=False, mirror=True)

# --- the red hexagon on each side just behind the plate ---------------------------------------------------------------
HEX = polygon([(math.cos(math.pi / 6 + i * math.pi / 3), math.sin(math.pi / 6 + i * math.pi / 3)) for i in range(6)])
hy, hz = -0.04, 0.29
k.decal((body_x(hy, hz), hy, hz), unit(1, -0.15, 0.3), (0.016, 0.016), RED, outline=HEX, eps=0.004, rings=8, segs=60,
        mirror=True)

# --- the mouth: a small frown over the jutting lip ------------------------------------------------------------------
CT, INK = srgb(TAN), srgb(INKH)


def frown(u, v):
    c = np.tile(CT, (len(u), 1))
    c[(np.abs(v - (0.35 - 0.6 * u * u)) < 0.3) & (np.abs(u) < 0.88)] = INK
    return c


k.decal((0.0, -0.3, 0.212), unit(0, -1, 0.15), (0.038, 0.016), frown, eps=0.004, rings=10, segs=48)

# --- the eyes: tired dashes on the plate's sides, brought forward and turned some 40 degrees from straight ahead -----


def dash(u, v):
    c = np.tile(CT, (len(u), 1))
    c[(np.abs(u) < 0.9) & (np.abs(v - 0.12 * u) < 0.42)] = INK
    return c


def tired(state):
    if state != "Fierce":
        return eye_look(state, dash, CT, INK, line=0.4)

    def look(u, v):
        c = np.tile(CT, (len(u), 1))
        c[(np.abs(u) < 0.9) & (np.abs(v - (0.55 * u - 0.05)) < 0.42)] = INK
        return c
    return look


EYE_N = unit(0.78, -0.6, 0.12)


def eyes(state):
    k.decal((0.07, -0.215, 0.262), EYE_N, (0.034, 0.017), tired(state), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.0055, smooth=4, band=0.006)
k.eye_states(eyes)
k.export(out)
