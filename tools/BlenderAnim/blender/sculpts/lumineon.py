# Lumineon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lumineon.py -- <the export's Lumineon.glb> <out.glb>
#
# Grown from finneon.py (its colours, its eye and its smile). From the references: a deep, rounded teardrop of a fish,
# a little longer than it is tall and flat from side to side, a small rounded snout with a tiny smile at the front
# and a short stalk behind ending in a round navy lobe with a pink oval spot; dark navy but for a pale sky-blue face
# over the front third and one pale ring behind it; heavy-lidded pink eyes; two great fins rising from the top of
# the back like a butterfly's wings, each about as long as the body and half as wide as it is long, a curved leading
# edge, a broad sail and a rounded tip curling outward, navy on the inner two thirds with a ragged boundary to the
# pale outer edge and tip; two small pale fins of two round lobes each under the belly. The wings are bent sheets
# with a little thickness, their pale part a thicker sheet over the same surface, so it shows on both faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, SKY, PINK, INK = "#1C3A60", "#9ED8F2", "#EE6EA6", "#141C2A"

# --- the body: a deep rounded teardrop, flat from side to side ----------------------------------------------------
ZC = 0.256                 # the body's axis
RX, RY, RZ = 0.064, 0.108, 0.114
YC = 0.0


def taper(V):
    """The unit sphere narrowed toward the tail (+y) and a little toward the snout (-y)."""
    V = V.copy()
    y = V[:, 1]
    s = np.where(y > 0, 1 - 0.3 * y * y, 1 - 0.06 * y * y)
    V[:, 0] *= s; V[:, 2] *= s
    return V


def snout(V):
    """The head's front drawn to a small rounded point a little under the axis."""
    V = V.copy()
    f = np.clip(-V[:, 1], 0, None)
    s = 1 - 0.5 * f ** 1.6
    V[:, 0] *= s; V[:, 2] = V[:, 2] * s - 0.1 * f ** 2
    return V


k.add("body", ell((0, YC, ZC), (RX, RY, RZ), deform=taper), NAVY, "body")
k.add("head", ell((0, -0.06, ZC - 0.008), (0.056, 0.088, 0.09), deform=snout), NAVY, "head")
k.add("stalk", tube([(0, 0.07, ZC + 0.006), (0, 0.13, ZC + 0.016), (0, 0.18, ZC + 0.03)], [0.055, 0.028, 0.017]),
      NAVY, "tail")


def band(P):
    """Distance back along the body, the bands bowed forward at the top and bottom like a ring round it."""
    return P[:, 1] - 2.2 * ((P[:, 2] - ZC) ** 2 + P[:, 0] ** 2)


FACE = -0.082              # the pale face in front of this (at the axis; further forward over and under it)
RING0, RING1 = -0.052, -0.038
k.paint(lambda P: inside(band(P) - FACE, 0.0025), SKY)
k.paint(lambda P: inside(np.abs(band(P) - (RING0 + RING1) / 2) - (RING1 - RING0) / 2, 0.0025), SKY)

# The tiny smile on the snout's front, under its point
def mouth(u, v):
    c = np.tile(srgb(SKY), (len(u), 1))
    c[np.abs(v - (0.85 * u * u - 0.3)) < 0.3] = srgb(INK)
    return c
k.decal((0, -0.153, ZC - 0.026), unit(0, -1, -0.45), (0.022, 0.01), mouth, eps=0.004, rings=8, segs=40)

# --- the great fins: broad bent sails rising from the top of the back like a butterfly's wings ---------------------
# Laid out first in a flat plane (a back along the body, b up), then leant out and opened, and the tip curled out.
TILT = math.radians(18)                     # leant out from the upright
YAW = math.radians(42)                      # opened outward behind, as seen from above
EY = np.array([math.sin(YAW), math.cos(YAW), 0.0])
EZ = np.array([math.sin(TILT), 0.0, math.cos(TILT)])
NRM = np.cross(EY, EZ); NRM /= np.linalg.norm(NRM)     # out and down: the way the tip curls
WX = 0.022                                  # where the root lies off the midline


def back_z(x, a):
    """The top of the body at (x, a), so the wing's root lies in the back."""
    t = np.clip((a - YC) / RY, -1, 1)
    s = np.where(t > 0, 1 - 0.3 * t * t, 1 - 0.06 * t * t)
    q = np.clip(1 - t * t - (x / (RX * s)) ** 2, 0, 1)
    return ZC + RZ * s * np.sqrt(q)


def outline(vv):
    """The leading and trailing edges in the wing's plane at a height vv (0 root, 1 tip), and the height."""
    lead = -0.03 + 0.13 * vv ** 1.8 - 0.012 * np.sin(math.pi * vv)     # rising steeply, then swept back: curved
    trail = 0.07 + 0.07 * vv + 0.055 * np.sin(math.pi * vv ** 0.8)      # the broad sail, out behind
    # the tip a round lobe: the chord closes on a circle over the last fifth
    r = np.where(vv > 0.72, np.sqrt(np.clip(1 - ((vv - 0.72) / 0.29) ** 2, 0, 1)), 1.0)
    mid = np.where(vv > 0.72, lead + (trail - lead) * 0.62, (lead + trail) / 2)
    lo = mid + (lead - mid) * r
    hi = mid + (trail - mid) * r
    b = 0.25 * vv
    return lo, hi, b


def wing_at(u, v):
    """u from the leading edge (0) to the trailing edge (1), v from the root (0) to the tip (1)."""
    vv = 0.012 + 1.0 * v
    lo, hi, b = outline(vv)
    a = lo + (hi - lo) * u
    root_a = -0.035 + 0.12 * u                                            # where this line of the wing leaves the back
    base_z = back_z(WX, root_a) - 0.014
    P = (np.stack([np.full_like(u, WX), root_a, base_z], axis=-1)
         + ((a - root_a)[..., None]) * EY + b[..., None] * EZ)
    # the sail cups a little, and the outer part curls out and over to the side
    bend = 0.035 * vv ** 2.2 - 0.012 * np.sin(math.pi * u) * vv
    return P + bend[..., None] * NRM


def pale_from(v):
    """Where the pale outer part begins across the wing: a ragged line, the whole width toward the tip."""
    vv = 0.012 + 1.0 * v
    saw = 2 * np.abs((vv * 5.5) % 1.0 - 0.5)
    ragged = 0.6 + 0.12 * saw
    return ragged * (1 - smoothstep(0.62, 0.78, vv))


def pale_at(u, v):
    u0 = pale_from(v)
    u0 = u0 - 0.03 * smoothstep(0.55, 0.75, 0.012 + v)
    return wing_at(u0 + (1.02 - u0) * u, v * 1.012)


def lead_at(u, v):
    """A pale edge along the leading edge's outer half, as the references have it."""
    w = 0.02 + 0.07 * smoothstep(0.0, 0.5, v)
    return wing_at(w * u - 0.02, 0.45 + 0.555 * v)


k.add("wing", sheet(wing_at, 22, 36, 0.009), NAVY, "fin{s}", fuse=False, mirror=True)
k.add("wingpale", sheet(pale_at, 12, 36, 0.0135), SKY, "fin{s}", fuse=False, mirror=True)
k.add("winglead", sheet(lead_at, 4, 24, 0.0135), SKY, "fin{s}", fuse=False, mirror=True)

# Small pale fins under the belly, each of two round lobes hanging down and splayed
for i, (dy, rx) in enumerate(((-0.016, -26), (0.016, 26))):
    k.add(f"low{i}", ell((0.03, -0.03 + dy, 0.142), (0.009, 0.017, 0.024), rot=(rx, 0, -12)), SKY, "fin2{s}",
          fuse=False, mirror=True)

# The tail's round lobe, navy with a pink spot on each face, turned toward the right and back so its faces show
LOBE = (0, 0.218, ZC + 0.06)
LR = (-35, 0, 38)
k.add("paddle", ell(LOBE, (0.012, 0.048, 0.044), rot=LR), NAVY, "tailFin", fuse=False)
m = np.array([[1, 0, 0]], float)
from mathutils import Euler
R = np.array(Euler([math.radians(a) for a in LR]).to_matrix())
nrm = R @ np.array([1.0, 0, 0])
for s in (1, -1):
    c = np.array(LOBE) + nrm * 0.0085 * s + R @ np.array([0, 0.006, 0.004])
    k.add(f"spot{s}", ell(tuple(c), (0.005, 0.027, 0.018), rot=LR), PINK, "tailFin", fuse=False)

# --- the eyes: Finneon's pink eye under a heavy lid, on the side of the head at the face's edge ---------------------
CN, CS, CP, CK, CW = srgb(NAVY), srgb(SKY), srgb(PINK), srgb(INK), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    r2 = u * u + v * v
    c = np.tile(CP, (len(u), 1))
    c[(u / 0.48) ** 2 + ((v + 0.1) / 0.58) ** 2 < 1] = CK
    c[((u + 0.12) / 0.17) ** 2 + ((v + 0.02) / 0.17) ** 2 < 1] = CW
    c[r2 > 0.8] = CK
    # the heavy lid, flat across the top
    c[v > 0.28] = CS
    c[np.abs(v - 0.28) < 0.14] = CK
    return c
def look(state):
    # The lid is the pale face the eye sits in, its edge a dark line
    return eye_look(state, eye, CS, CK, line=0.17)
de = unit(0.8, -0.56, 0.12)
def eyes(state):
    k.decal(np.array([0, -0.098, ZC + 0.01]) + de * 0.06, de, (0.024, 0.025), look(state), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0045, smooth=4)
k.eye_states(eyes)
k.export(out)
