# Delibird sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/delibird.py -- <the export's Delibird.glb> <out.glb>
# A red penguin-like bird, head and body one egg: a white face and a white beard falling in points down the chest,
# two spiky white tufts standing up and out from the brow, round eyes in thick black rims with black pupils turned
# in, a small yellow beak pointing down, a white button low on the belly, red flipper wings (the right one reaching
# back to its tail), the tail a large pale sack low behind the right hip with its gathered mouth at the hand, short
# yellow feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, lumpy, srgb, inside, unit, eye_look, args, sibling

p = sibling("pidgey")
RED, WHITE, SACK, MOUTH, YELLOW = "#E2363A", "#F2F3F6", "#CFD2DA", "#EEEFF3", "#F2C53A"
INK = srgb("#121214")

C = np.array([0, 0.0, 0.282])            # the egg's centre, over the body's joint (0, 0, 0.3)
CR = np.array([0.14, 0.132, 0.25])
R = 0.12                                # the head's radius for its round coordinates
HY = 0.0                                # the egg's upright axis


def egg(V):
    """Narrower above, its foot flattened a little."""
    V = V.copy()
    t = (1 - V[:, 2]) / 2
    V[:, 0] *= 0.68 + 0.36 * t
    V[:, 1] *= 0.74 + 0.32 * t
    low = V[:, 2] < 0
    V[low, 2] = -(np.clip(-V[low, 2], 0, 1) ** 0.62)
    return V


def round_head(P):
    P = np.atleast_2d(P)
    return np.arctan2(P[:, 0], -(P[:, 1] - HY)) * R, P[:, 2]


def beak_shape(V):
    """A short wedge pointing down: wide at its top, narrowing to a point below."""
    t = np.clip(-V[:, 2], 0, 1)
    V = V.copy()
    V[:, 0] *= 1 - 0.75 * t
    V[:, 1] *= 1 - 0.35 * t
    V[:, 2] *= 1 + 0.5 * t
    return V


src, out = args()
k = Kit(src, coat="fur")

# The egg, the face standing a little forward of it
k.add("body", ell(C, CR, deform=egg), RED, "body")
k.add("face", ell((0, -0.035, 0.43), (0.098, 0.075, 0.085)), RED, "head")

# The white face and beard: round the head's axis, from the brow down to the beard's points
FACE = p.mirrored([(0.0, 0.535), (0.06, 0.53), (0.105, 0.5), (0.128, 0.45), (0.152, 0.4), (0.13, 0.385),
                   (0.15, 0.33), (0.118, 0.325), (0.122, 0.265), (0.088, 0.27), (0.072, 0.21), (0.044, 0.245),
                   (0.0, 0.185)])
k.paint(lambda P: p.poly_mask(*round_head(P), FACE) * (P[:, 1] < 0.0), WHITE)

# The tufts: two fans of thick white points standing up and out from the brow, on white cushions
for s in (1, -1):
    b = np.array([0.065 * s, -0.01, 0.5])
    k.add("tuftbase", ell(b, (0.05, 0.045, 0.04)), WHITE, "head")
    for tip, w in (((0.15, 0.0, 0.615), 0.078), ((0.06, -0.005, 0.63), 0.068), ((0.18, 0.01, 0.53), 0.064)):
        t = np.array([tip[0] * s, tip[1], tip[2]])
        k.add("tuft", p.spike(b, t, w, 0.042, side=(0, 1, 0)), WHITE, "head", fuse=False)
    # the fluff at the cheeks and the beard's sides
    for base, tip in (((0.085, -0.07, 0.42), (0.16, -0.065, 0.38)), ((0.09, -0.07, 0.34), (0.162, -0.06, 0.31))):
        k.add("fluff", p.spike(np.array([base[0] * s, base[1], base[2]]), np.array([tip[0] * s, tip[1], tip[2]]),
                               0.04, 0.03, side=(0, 0, 1)), WHITE, "head", fuse=False)

# The beak: small, yellow, pointing down
k.add("beak", ell((0, -0.118, 0.41), (0.034, 0.03, 0.028), rot=(-12, 0, 0), deform=beak_shape), YELLOW, "head",
      fuse=False, surface="shell")

# The button low on the belly
k.decal((0, -0.13, 0.12), unit(0, -1, -0.15), (0.017, 0.02), WHITE, eps=0.004, rings=8, segs=32)

# The wings: red flippers, the left forward and down, the right reaching back to the sack's mouth
k.add("armL", blade([(-0.1, 0.0, 0.37), (-0.15, -0.03, 0.3), (-0.168, -0.06, 0.22)], [0.056, 0.048, 0.026],
                    (1, 0.2, 0), 0.45), RED, "armL")
k.add("armR", blade([(0.1, 0.0, 0.37), (0.155, 0.03, 0.29), (0.17, 0.07, 0.215)], [0.056, 0.048, 0.026],
                    (1, -0.2, 0), 0.45), RED, "armR")

# The tail: a large pale sack low behind the right hip, its gathered mouth at the hand
k.add("sack", ell((0.115, 0.17, 0.145), (0.09, 0.115, 0.088), rot=(0, 0, -30)), SACK, "tail")
k.add("sackmouth", ell((0.165, 0.085, 0.2), (0.042, 0.036, 0.036), deform=lumpy(5, 3, 0.18)), MOUTH, "tail",
      fuse=False)

# The feet: short stubs under the egg on wide yellow feet of three rounded toes
def toes(V):
    ph = np.arctan2(V[:, 0], -V[:, 1])
    front = np.clip(-V[:, 1], 0, 1)
    s = 1 + 0.18 * np.cos(3 * ph) * front
    V = V.copy(); V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("leg", tube([(0.075, 0.0, 0.11), (0.078, -0.01, 0.035)], [0.028, 0.024]), RED, "leg{s}", fuse=False, mirror=True)
k.add("foot", ell((0.08, -0.055, 0.022), (0.05, 0.072, 0.022), rot=(0, 0, 12), deform=toes), YELLOW, "leg{s}",
      fuse=False, mirror=True, surface="shell")

# The eyes: round, a thick black rim kept in every state, a white ring, a black pupil turned in toward the beak
WHITEC = np.array([1.0, 1.0, 1.0])
def eye_open(u, v):
    c = np.tile(WHITEC, (len(u), 1))
    c[((u + 0.16) / 0.4) ** 2 + ((v + 0.02) / 0.44) ** 2 < 1] = INK
    return c
def eyes(state):
    look = eye_look(state, eye_open, WHITEC, INK, line=0.16)
    def rimmed(u, v):
        c = look(u * 1.45, v * 1.45)
        c[u * u + v * v > 0.69 ** 2] = INK
        return c
    k.decal((0.048, -0.11, 0.455), unit(0.42, -0.9, 0.08), (0.036, 0.036), rimmed, eps=0.006, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.006, band=0.004)
k.eye_states(eyes)
k.export(out)
