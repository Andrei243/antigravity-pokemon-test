# Mantine sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mantine.py -- <the export's Mantine.glb> <out.glb>
# Grown from mantyke.py: the same domed body with its face on the front, the wings laid on the fins the same way and
# coloured by the same line along their middle, the eyes made the same way, smaller.
#
# From the references: a great manta. A round body, navy on top and pale lilac beneath, the lilac face up to a little
# over the eyes; wide diamond wings swept back to pointed tips that lift a little, navy on top, lilac beneath, the
# navy running over the leading edge onto the underside in a band of sawteeth; two navy head fins rising from the top
# of the head like broad rounded paddles; small round black eyes with a white glint; a small cat's mouth; three gill
# slits on each lower side; a long thin pale tail ending in a flat fin. The Remoraid often drawn under its wing is
# left out.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, tri, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, LILAC, TAIL, INK = "#2C3C6C", "#D8CCEA", "#B4C6EA", "#161A2C"

# ---- the body: a round dome, longer than wide, the face a big rounded front on it
k.add("body", ell((0, 0.02, 0.115), (0.13, 0.17, 0.085)), NAVY, "body")
k.add("face", ell((0, -0.085, 0.112), (0.118, 0.095, 0.082)), NAVY, "head")

# ---- the wings: wide diamonds from the body's sides, swept back to pointed tips that lift a little, thinning out
WC, WR = np.array([0.29, 0.035, 0.115]), np.array([0.2, 0.15, 0.028])
P_, RISE, ARCH, SWEEP = 1.3, -1.2, 6.0, 0.75
def pitch(t):                       # the leading edge held high, so the lilac underside faces forward as in the art
    return 2.6 * (0.6 + 0.8 * t)
def diamond(V):
    V = V.copy()
    r2 = np.hypot(V[:, 0], V[:, 1])
    rp = (np.abs(V[:, 0]) ** P_ + np.abs(V[:, 1]) ** P_) ** (1 / P_)
    s = np.where(rp > 1e-6, r2 / np.maximum(rp, 1e-6), 1.0)
    V[:, 0] *= s; V[:, 1] *= s
    t = np.clip((V[:, 0] + 0.6) / 1.6, 0, 1)
    V[:, 2] *= 1 - 0.4 * t
    V[:, 2] += RISE * t * t + ARCH * t * (1 - t) - pitch(t) * V[:, 1]
    V[:, 1] += SWEEP * t * t
    return V
k.add("wing", ell(WC, WR, deform=diamond, sub=5), NAVY, "fin{s}", mirror=True)

def wing_t(ax):
    return np.clip(((ax - WC[0]) / WR[0] + 0.6) / 1.6, 0, 1)
def wing_mid(ax, y):
    t = wing_t(ax)
    yu = (y - WC[1]) / WR[1] - SWEEP * t * t
    return WC[2] + WR[2] * (RISE * t * t + ARCH * t * (1 - t) - pitch(t) * yu)
def wing_front(ax):
    xu = np.clip((ax - WC[0]) / WR[0], -1, 1)
    yu = -(1 - np.abs(xu) ** P_) ** (1 / P_)
    return WC[1] + WR[1] * (yu + SWEEP * wing_t(ax) ** 2)

# Lilac beneath the line along the wings' middle and over the face, a little above the eyes
def lilac(P):
    ax, y, z = np.abs(P[:, 0]), P[:, 1], P[:, 2]
    face = smoothstep(-0.03, -0.12, y) * (1 - smoothstep(0.1, 0.16, ax))
    line = (wing_mid(ax, y) - 0.005) * (1 - face) + 0.172 * face
    return inside(z - line, 0.003)
k.paint(lilac, LILAC)
# The navy running over each wing's leading edge onto the underside, its back edge cut into sawteeth
def teeth(P):
    ax, y = np.abs(P[:, 0]), P[:, 1]
    xu = (ax - WC[0]) / WR[0]
    depth = WR[1] * (0.32 + 0.2 * tri(xu * 2.6 + 0.25))
    wing = smoothstep(0.13, 0.17, ax)
    return inside(y - (wing_front(ax) + depth), 0.0025) * wing
k.paint(teeth, NAVY)

# ---- the head fins: broad rounded paddles rising from the top of the head, flat front to back
FIN = [(0.04, -0.11, 0.175), (0.048, -0.112, 0.23), (0.057, -0.104, 0.275), (0.062, -0.09, 0.3)]
k.add("headfin", blade(FIN, [0.017, 0.022, 0.024, 0.017], (0, 1, 0), 0.55), NAVY, "head", mirror=True)

# ---- the tail: long and thin, pale, ending in a flat fin
TL = [(0, 0.17, 0.1), (0.0, 0.26, 0.093), (0.02, 0.34, 0.085), (0.05, 0.41, 0.083)]
k.add("tail", tube(TL, [0.016, 0.01, 0.007, 0.006]), TAIL, "tail", fuse=False, surface="skin")
k.add("tailfin", blade([(0.045, 0.4, 0.083), (0.07, 0.45, 0.084), (0.1, 0.5, 0.086), (0.125, 0.535, 0.09)],
                       [0.008, 0.022, 0.02, 0.004], (0, 0, 1), 0.3), TAIL, "tail", fuse=False, surface="skin")

# ---- the mouth: a small cat's mouth, one ink line of two curves meeting in the middle
CK, CL = srgb(INK), srgb(LILAC)
def mouth(u, v):
    c = np.tile(CL, (len(u), 1))
    c[(np.abs(v - (0.55 * np.abs(np.abs(u) - 0.5) - 0.15)) < 0.2) & (np.abs(u) < 0.95)] = CK
    return c
k.decal((0, -0.18, 0.13), unit(0, -1, 0.15), (0.026, 0.014), mouth, eps=0.004, rings=12, segs=48)

# ---- three gill slits on each lower side of the front
def gills(u, v):
    c = np.tile(CL, (len(u), 1))
    for v0 in (-0.6, 0.0, 0.6):
        c[(np.abs(v - v0 - 0.15 * u * u) < 0.16) & (np.abs(u) < 0.85)] = CK
    return c
k.decal((0.09, -0.125, 0.09), unit(0.6, -0.7, -0.35), (0.024, 0.02), gills, eps=0.004, rings=14, segs=48,
        mirror=True)

k.build(voxel=0.005, smooth=5)

# ---- the eyes: small and round, black with a white glint toward the top inner side. u is outward on both.
CW = np.array([1.0, 1.0, 1.0])
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    c[np.sqrt(u * u + v * v) < 0.82] = CK
    c[((u + 0.2) ** 2 + (v - 0.24) ** 2) < 0.28 ** 2] = CW
    return c
def eyes(state):
    dn = unit(0.35, -0.9, 0.25)
    k.decal((0.06, -0.165, 0.158), dn, (0.021, 0.022), eye_look(state, eye, CL, CK, line=0.17), eps=0.005,
            rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
