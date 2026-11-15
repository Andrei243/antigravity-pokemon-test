# Luvdisc sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/luvdisc.py -- <the export's Luvdisc.glb> <out.glb>
#
# From the references: a pink fish that is a heart seen from the side, flat from side to side but chunky: the heart's
# point is its nose, with pale pouting lips on it (a short dark line across their tip), and its two round lobes are the
# back, the upper one larger; a small black eye with a glint a little behind and over the lips and a pale pink oval
# cheek behind and under the eye; no fins. The game never sees a model side-on, so the heart's plane is turned 45
# degrees about the upright: its nose to the front and the model's -X, one face toward the front three-quarter camera
# and the other toward the camera behind. The joints: body (0, 0, 0.18), tail (0, 0.03, 0.2).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

PINK, LIP, CHEEK, INKH, MOUTH = "#EE8A9E", "#F7D6DC", "#F8CBD4", "#181822", "#B83A56"

# --- the heart's frame: a back along it from the nose, b up, n out of its +X face ------------------------------------
TURN = math.radians(45)
A = np.array([math.sin(TURN), math.cos(TURN), 0.0])      # from the nose back along the heart
Z = np.array([0.0, 0.0, 1.0])
N = np.cross(A, Z)                                       # the face the front three-quarter camera sees
MID = 0.105                                              # the heart's middle, this far back from the nose
O = np.array([0.0, 0.0, 0.18]) - A * MID                 # the nose, so the heart's middle is on the body joint
THICK = 0.062                                            # half the heart's thickness at its fullest


def at(a, b, n=0.0):
    return O + a * A + b * Z + n * N


def local(shape):
    """A shape made in the heart's own (a, b, n) put in its place."""
    V, F = shape
    return O + V[:, :1] * A + V[:, 1:2] * Z + V[:, 2:] * N, F


def lobe(c, long, short, angle):
    """A lobe of the heart: an ellipsoid along a direction angle degrees up from straight back."""
    return local(ell((c[0], c[1], 0.0), (long, short, THICK), rot=(0, 0, angle)))


# --- the heart: two lobes meeting at the nose, the upper one larger --------------------------------------------------
k.add("upper", lobe((0.12, 0.082), 0.155, 0.098, 36), PINK, "body")
k.add("lower", lobe((0.118, -0.068), 0.115, 0.076, -32), PINK, "body")
k.add("fill", local(ell((0.09, 0.0, 0.0), (0.08, 0.07, THICK * 0.95))), PINK, "body")
# The notch between the lobes is the tail's
k.add("notch", local(ell((0.16, 0.0, 0.0), (0.03, 0.03, THICK * 0.45))), PINK, "tail")

# --- the lips: pale, pouting forward off the point, a short dark line across their tip -------------------------------
LIP_C = np.array([-0.012, -0.006])
k.add("lips", local(ell((LIP_C[0], LIP_C[1], 0.0), (0.036, 0.028, 0.03), rot=(0, 0, -4))), LIP, "body")


def mouthline(P):
    d = P - O
    a, b = d @ A, d @ Z
    return inside(np.abs(b - LIP_C[1]) - 0.0028, 0.0015) * inside(a - (LIP_C[0] - 0.012), 0.002)


k.paint(mouthline, MOUTH, on=["lips"])

# --- the cheeks: a pale pink oval on each face behind and under the eye ----------------------------------------------
for s in (1, -1):
    k.decal(at(0.112, -0.015, s * THICK * 0.8), s * N, (0.018, 0.023), CHEEK, eps=0.004, rings=12, segs=48)

# --- the eyes: small and black, a dark blue low in them and a white glint up front -----------------------------------
CK, CB, CW, CP = srgb(INKH), srgb("#24306A"), np.array([1.0, 1.0, 1.0]), srgb(PINK)


def eye(u, v):
    c = np.tile(CK, (len(u), 1))
    c[(u * u + (v + 0.55) ** 2 < 0.3)] = CB
    c[(u * u + v * v > 0.9)] = CK
    c[((u + 0.25) / 0.3) ** 2 + ((v - 0.3) / 0.32) ** 2 < 1] = CW
    return c


def eyes(state):
    for s in (1, -1):
        n = unit(*(s * N * 0.94 - A * 0.34))
        # u runs back along the heart on both faces: the glint (at -u) is toward the nose
        f = eye_look(state, eye, CP, CK, line=0.18)
        look = f if s > 0 else (lambda u, v, f=f: f(-u, v))      # on the far face +u runs forward: turn it back
        k.decal(at(0.085, 0.022, s * THICK * 0.8), n, (0.016, 0.021), look, up=(0, 0, 1), eps=0.004, rings=20,
                segs=56)


k.build(voxel=0.0042, smooth=4, band=0.006)
k.eye_states(eyes)
k.export(out)
