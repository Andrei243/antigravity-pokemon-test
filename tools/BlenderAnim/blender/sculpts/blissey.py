# Blissey sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py), grown from
# Chansey's script (chansey.py): the same pink, white egg and deeper pink pouch, small dark eyes, the egg of a body
# grown round and dressed.
#   blender --background --factory-startup --python sculpts/blissey.py -- <the export's Blissey.glb> <out.glb>
#
# The design, from the references: a round body, pink above and white below like a gown, with no head of its own;
# a cloud of pink hair in round puffs over the top and a curl wound like a snail's shell at each side of the face;
# small dark upright eyes high on the front, close together, and an open smile with a tongue under them; short pink
# arms sticking out at the sides; white feathered frills, a fan of them behind each arm and a hem of them round the
# hips at the gown's edge; the white egg in a deeper pink pouch on the belly; short pink legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, leaf, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PINK, HAIR, DEEP, LEGS, WHITE = "#F6B4C8", "#F4A6C2", "#EE7A96", "#F2A2BC", "#FAF8F4"
FRILL, INKH, MOUTHH, TONGUEH = "#F4F4F0", "#1C1A22", "#7A2E48", "#EE7A86"
C = np.array([0, 0.0, 0.405])             # the round body's middle
R = np.array([0.3, 0.28, 0.315])
HEM = 0.33                               # where the white gown begins


def round_(V):
    """Round, a little fuller below its middle."""
    V = V.copy()
    s = 1 + 0.04 * np.clip(-V[:, 2], 0, 1) - 0.04 * np.clip(V[:, 2], 0, 1) ** 2
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell(C, R, deform=round_), PINK, "body")
# the top of the body on the head's bone, so the face and the hair move with it
k.add("crown", ell((0, 0.0, 0.62), (0.2, 0.19, 0.1)), PINK, "head")

# The white gown: everything of the body under the hem, a crisp line kept off the legs
def gown(P):
    return inside(P[:, 2] - HEM, 0.003) * (1 - inside(P[:, 2] - 0.097, 0.003))
k.paint(gown, WHITE)

# Short pink legs out of the body's bottom, rounded at their ends
k.add("leg", tube([(0.1, -0.02, 0.17), (0.103, -0.03, 0.08), (0.105, -0.035, 0.04)], [0.056, 0.052, 0.05]), LEGS,
      "leg{s}", mirror=True)
k.paint(lambda P: inside(P[:, 2] - 0.097, 0.003), LEGS)

# Short stubby arms sticking out at the sides, a little forward and down
k.add("arm", tube([(0.2, -0.1, 0.43), (0.29, -0.13, 0.415), (0.355, -0.15, 0.395)], [0.06, 0.045, 0.032]), PINK,
      "arm{s}", mirror=True)

# Hair: a cloud of round pink puffs over the top, and a snail's-shell curl at each side of the face
for i, (c, r) in enumerate([((0.12, 0.02, 0.69), 0.115), ((-0.12, 0.02, 0.695), 0.12), ((0.0, 0.06, 0.715), 0.11),
                            ((0.13, 0.13, 0.64), 0.1), ((-0.13, 0.13, 0.64), 0.1), ((0.0, 0.16, 0.66), 0.1)]):
    k.add(f"puff{i}", ell(c, (r, r * 0.95, r * 0.9)), HAIR, "head")
CC = np.array([0.215, -0.13, 0.6])               # the side curl's middle
n = unit(0.55, -0.83, 0.0)                        # it faces out and forward
k.add("curlball", ell(CC - n * 0.03, (0.066, 0.062, 0.068)), HAIR, "head", mirror=True)
e1 = np.array([0, 0, 1.0]); e2 = np.cross(n, e1)
pts, rad = [CC - n * 0.02 + (0, 0, 0.062), CC + n * 0.008 + (0, 0, 0.064)], [0.022, 0.021]
for t in np.linspace(0, 1, 20):
    a = math.pi * 0.5 - t * 2 * math.pi * 1.3
    r = 0.064 * (1 - 0.82 * t)
    pts.append(CC + n * (0.012 + 0.022 * t) + r * (math.sin(a) * e1 + math.cos(a) * e2))
    rad.append(0.021 * (1 - 0.45 * t))
k.add("curl", tube(pts, rad), HAIR, "head", fuse=False, mirror=True)

# White feathered frills: a fan behind each arm, flat to the front, and a hem round the hips at the gown's edge
for i, (tip, w) in enumerate([((0.4, 0.04, 0.66), 0.04), ((0.44, 0.03, 0.57), 0.042), ((0.43, 0.02, 0.49), 0.036)]):
    base = np.array([0.22, 0.04, 0.47])
    tip = np.array(tip)
    p = [base, base + (tip - base) * 0.35, base + (tip - base) * 0.75, tip]
    k.add(f"wing{i}", blade(p, [0.018, w, w * 0.85, 0.022], (0, 1, 0), 0.34), FRILL, "body", fuse=False, mirror=True)
for i, ang in enumerate([42, 70, 98, 126, 154]):
    a = math.radians(ang)                         # from the front, round toward the back
    o = np.array([math.sin(a), -math.cos(a), 0.0])
    base = np.array([0, 0, HEM + 0.005]) + o * 0.27
    d = o * math.cos(math.radians(44)) + np.array([0, 0, -math.sin(math.radians(44))])
    t = np.cross((0, 0, 1.0), o)
    flat = np.cross(d, t)
    p = [base - d * 0.02, base + d * 0.05, base + d * 0.12, base + d * 0.165]
    k.add(f"hem{i}", blade(p, [0.022, 0.05, 0.046, 0.022], flat, 0.3), FRILL, "body", fuse=False, mirror=True)

# The white egg in its deeper pink pouch on the belly (loose: the pouch's straight top edge and the egg stay crisp)
k.add("egg", ell((0, -0.275, 0.27), (0.092, 0.075, 0.11), rot=(-8, 0, 0)), WHITE, "body", fuse=False,
      surface="shell")
def pouch(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.22)          # cut straight across at its top
    return V
k.add("pouch", ell((0, -0.255, 0.228), (0.12, 0.092, 0.098), rot=(-6, 0, 0), deform=pouch), DEEP, "body",
      fuse=False)

# The open smile, carved under the eyes, with a tongue at its bottom
INK, MOUTH, LID = srgb(INKH), srgb(MOUTHH), srgb(PINK)
fs = unit(0, -0.82, 0.57)
MC = np.array([0, 0, 0.44]) + unit(0, -0.88, 0.47) * 0.27
def d_shape(V):
    V = V.copy()
    V[:, 2] = np.minimum(V[:, 2], 0.3)
    return V
k.add("mouth", ell(MC, (0.036, 0.022, 0.03), rot=(-28, 0, 0), deform=d_shape), MOUTH, "head", carve=True)
k.add("tongue", ell(MC + (0, 0.01, -0.012), (0.022, 0.012, 0.009), rot=(-28, 0, 0)), TONGUEH, "head", fuse=False)

# The eyes: small, dark and upright, close together high on the front, a glint toward the outer side
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    c[(u / 0.52) ** 2 + (v / 0.88) ** 2 < 1] = INK
    c[((u - 0.1) / 0.22) ** 2 + ((v - 0.4) / 0.22) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.18, -0.8, 0.57)
E = np.array([0, 0, 0.44]) + de * 0.3
def eyes(state):
    k.decal(E, de, (0.025, 0.03), eye_look(state, eye, LID, INK, line=0.16), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0075)
k.eye_states(eyes)
k.export(out)
