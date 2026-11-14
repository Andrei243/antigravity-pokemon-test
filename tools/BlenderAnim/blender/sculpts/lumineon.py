# Lumineon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lumineon.py -- <the export's Lumineon.glb> <out.glb>
#
# Grown from finneon.py (its colours, its eye and its smile). From the references: a slender fish, its body a long
# oval thin from side to side, tapering forward to a small pointed snout with a tiny smile and behind to a slim tail
# stalk ending in a round navy paddle with a pink spot; dark navy all over but for a pale sky-blue face on the front
# and one thin pale ring behind the eyes; heavy-lidded pink eyes on the sides of the head; two great fins rising
# from the top of the back like a butterfly's wings, broad at the base, swept up and back, navy with a ragged pale
# band along the trailing edge and a rounded pale lobe at the tip; two small pale fins of two lobes each under the
# body. The wings are bent sheets with a little thickness, their pale band a thicker sheet over the same surface,
# so it shows on both faces.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, SKY, PINK, INK = "#1C3A60", "#9ED8F2", "#EE6EA6", "#141C2A"

# --- the body: a long oval, thin from side to side, pointed at the snout ------------------------------------------
ZC = 0.256                 # the body's axis
RX, RY, RZ = 0.047, 0.125, 0.085
YC = -0.005


def taper(V):
    """The unit sphere narrowed toward both ends: more toward the tail (+y), a little toward the snout (-y)."""
    V = V.copy()
    y = V[:, 1]
    s = np.where(y > 0, 1 - 0.28 * y * y, 1 - 0.12 * y * y)
    V[:, 0] *= s; V[:, 2] *= s
    return V


def snout(V):
    """The head's front drawn to a small rounded point a little under the axis."""
    V = V.copy()
    f = np.clip(-V[:, 1], 0, None)
    s = 1 - 0.55 * f ** 1.6
    V[:, 0] *= s; V[:, 2] = V[:, 2] * s - 0.12 * f ** 2
    return V


k.add("body", ell((0, YC, ZC), (RX, RY, RZ), deform=taper), NAVY, "body")
k.add("head", ell((0, -0.1, ZC - 0.004), (0.041, 0.075, 0.07), deform=snout), NAVY, "head")
k.add("stalk", tube([(0, 0.09, ZC + 0.004), (0, 0.16, ZC + 0.012), (0, 0.228, ZC + 0.024)], [0.042, 0.024, 0.016]),
      NAVY, "tail")


def band(P):
    """Distance back along the body, the bands bowed forward at the top and bottom like a ring round it."""
    return P[:, 1] - 3.2 * ((P[:, 2] - ZC) ** 2 + P[:, 0] ** 2)


FACE = -0.1                # the pale face in front of this (at the axis; further forward over and under it)
RING0, RING1 = -0.066, -0.053
k.paint(lambda P: inside(band(P) - FACE, 0.0025), SKY)
k.paint(lambda P: inside(np.abs(band(P) - (RING0 + RING1) / 2) - (RING1 - RING0) / 2, 0.0025), SKY)

# The tiny smile on the snout's front, under its point
def mouth(u, v):
    c = np.tile(srgb(SKY), (len(u), 1))
    c[np.abs(v - (0.85 * u * u - 0.3)) < 0.3] = srgb(INK)
    return c
k.decal((0, -0.168, ZC - 0.022), unit(0, -1, -0.45), (0.02, 0.009), mouth, eps=0.004, rings=8, segs=40)

# --- the great fins: bent sheets rising from the top of the back like a butterfly's wings ---------------------------
TILT = math.radians(42)                     # leant out from the upright
YAW = math.radians(28)                      # the wings opened outward behind, as seen from above
EY = np.array([math.sin(YAW), math.cos(YAW), 0.0])
EZ = np.array([math.sin(TILT), 0.0, math.cos(TILT)])
NRM = np.cross(EY, EZ); NRM /= np.linalg.norm(NRM)


def back_z(a):
    """The top of the body at a place along it, so the wing's root lies in the back."""
    t = np.clip((a - YC) / RY, -1, 1)
    s = np.where(t > 0, 1 - 0.28 * t * t, 1 - 0.12 * t * t)
    return ZC + RZ * s * np.sqrt(np.clip(1 - t * t, 0, 1))


def wing_at(u, v):
    """u from the leading edge (0) to the trailing edge (1), v from the root (0) to the tip (1)."""
    vv = 0.015 + 0.97 * v
    lead = -0.035 + 0.16 * vv ** 1.5 - 0.02 * np.sin(math.pi * vv)        # a leading edge hollowed near the root
    trail = 0.1 + 0.11 * vv + 0.02 * np.sin(math.pi * vv)                 # a trailing edge bowed out behind
    mid, half = (lead + trail) / 2, (trail - lead) / 2
    # the tip a round lobe: the chord closes on a circle over the last third
    r = np.where(vv > 0.78, np.sqrt(np.clip(1 - ((vv - 0.78) / 0.23) ** 2, 0, 1)), 1.0)
    # but no narrower than a bulb just under the tip
    a = mid + half * np.maximum(r, 0.0) * (2 * u - 1)
    b = 0.19 * vv + 0.02 * (1 - r) * (2 * u - 1) ** 2 * 0
    root_a = -0.035 + 0.135 * u                                           # where this line of the wing leaves the back
    base_z = back_z(root_a) - 0.016
    P = (np.stack([np.full_like(u, 0.012), root_a, base_z], axis=-1)
         + ((a - root_a)[..., None]) * EY + b[..., None] * EZ)
    # a slight curl: the wing cups a little and its tip leans out
    bend = -0.035 * vv ** 2 + 0.01 * (2 * u - 1) ** 2 * vv
    return P + bend[..., None] * NRM


def pale_from(v):
    """Where the pale trailing band begins across the wing: a ragged line, the whole width at the tip."""
    vv = 0.015 + 0.97 * v
    saw = 2 * np.abs((vv * 6.5) % 1.0 - 0.5)
    ragged = 0.64 + 0.09 * saw
    return ragged * (1 - smoothstep(0.66, 0.8, vv)) + 0.0 * smoothstep(0.66, 0.8, vv)


def pale_at(u, v):
    u0 = pale_from(v)
    return wing_at(u0 + (1 - u0) * (0.004 + 0.996 * u), v)


k.add("wing", sheet(wing_at, 18, 34, 0.009), NAVY, "fin{s}", fuse=False, mirror=True)
k.add("wingpale", sheet(pale_at, 10, 34, 0.0135), SKY, "fin{s}", fuse=False, mirror=True)

# Small pale fins under the body, each of two round lobes hanging down and splayed
for i, (dy, rx) in enumerate(((-0.014, -24), (0.014, 24))):
    k.add(f"low{i}", ell((0.026, -0.045 + dy, 0.17), (0.007, 0.013, 0.019), rot=(rx, 0, -10)), SKY, "fin2{s}",
          fuse=False, mirror=True)

# The tail's round paddle, navy with a pink spot on each face
k.add("paddle", ell((0, 0.272, ZC + 0.044), (0.011, 0.045, 0.04), rot=(-30, 0, 0)), NAVY, "tailFin", fuse=False)
for s in (1, -1):
    k.add(f"spot{s}", ell((0.0075 * s, 0.278, ZC + 0.047), (0.005, 0.026, 0.017), rot=(-30, 0, 0)), PINK, "tailFin",
          fuse=False)

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
de = unit(0.86, -0.48, 0.12)
def eyes(state):
    k.decal(np.array([0, -0.122, ZC + 0.008]) + de * 0.05, de, (0.022, 0.023), look(state), eps=0.005, rings=20,
            segs=56, mirror=True)

k.build(voxel=0.0045, smooth=4)
k.eye_states(eyes)
k.export(out)
