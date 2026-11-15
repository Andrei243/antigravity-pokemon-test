# Weedle sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/weedle.py -- <the export's Weedle.glb> <out.glb>
#
# From the references: a tan caterpillar of round segments, its front half reared up under a great round head (some
# 0.4 of the whole height). A big pink round nose on the front of the head, small black eyes with a white glint above
# and to the side of it, a white cone of a horn on the crown leaning a little back. Each segment a pair of pink beads
# low on its sides; creases between the segments. The last segment ends in a white stinger curving up.
# Skeleton: tail and body (0, 0.24, 0.058), seg1 (0, 0.155, 0.062), seg2 (0, 0.07, 0.074), seg3 (0, 0, 0.11),
# seg4 (0, -0.035, 0.175), head (0, -0.045, 0.225).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, outward, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

TAN, SEAM, PINK, STING, INK = "#D49A5C", "#9E6A3A", "#E27AA8", "#ECEEF2", "#16121A"

# --- the frame -------------------------------------------------------------------------------------------------------
# The body's centre line, from the tail's end to under the head, through the joints
PATH = np.array([(0, 0.305, 0.05), (0, 0.24, 0.056), (0, 0.155, 0.062), (0, 0.07, 0.072), (0, 0.0, 0.106),
                 (0, -0.035, 0.168), (0, -0.045, 0.222)], float)
JOINTS = [("tail", 0.3), ("body", 0.24), ("seg1", 0.155), ("seg2", 0.07), ("seg3", 0.0), ("seg4", -0.035)]
SEGS = 9
H = np.array([0.0, -0.058, 0.31])            # the head's middle
HR = np.array([0.1, 0.097, 0.097])

# the line sampled finely, with its arc length
_S = np.vstack([PATH[i] + (PATH[i + 1] - PATH[i]) * t for i in range(len(PATH) - 1)
                for t in np.linspace(0, 1, 40, endpoint=False)] + [PATH[-1:]])
_ARC = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(_S, axis=0), axis=1))])
_TS = np.gradient(_S, axis=0); _TS /= np.linalg.norm(_TS, axis=1, keepdims=True)


def at_arc(s):
    i = int(np.clip(np.searchsorted(_ARC, s), 0, len(_S) - 1))
    return _S[i], _TS[i]


def radius(t):
    """A segment's radius by its place from the tail (0) to the head (1)."""
    return 0.046 + 0.024 * t ** 0.9


def bone_of(p):
    """The joint nearest a point along the body (by y, which the body runs along)."""
    y = p[1]
    if y > 0.27:
        return "tail"
    best = min(JOINTS[1:], key=lambda j: abs(j[1] - y))
    return best[0]


# segments spaced along the line from a little in from the tail's end to just under the head
S0, S1 = 0.02, _ARC[-1] - 0.035
CENTRES = []
for i in range(SEGS):
    t = i / (SEGS - 1)
    s = S0 + (S1 - S0) * t
    c, d = at_arc(s)
    CENTRES.append((c, d, radius(t), s))


def add_body():
    for i, (c, d, r, s) in enumerate(CENTRES):
        # a little flattened along the body, so each reads as a disc of a segment
        ang = math.degrees(math.atan2(d[2], -d[1]))       # the tilt of the line from lying flat
        k.add(f"seg{i}", ell(c, (r * 1.02, r * 0.74, r), rot=(-ang, 0, 0)), TAN, bone_of(c))
    k.add("head", ell(H, HR), TAN, "head")
    # a neck filling the crease under the head
    k.add("neck", ell(H + (0, 0.02, -0.075), (0.06, 0.055, 0.04)), TAN, "head")


def beads():
    """A pair of pink beads low on the sides of each segment."""
    for i, (c, d, r, s) in enumerate(CENTRES):
        side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
        side = np.abs(side[0]) * np.array([1.0, 0, 0])
        down = np.cross(side, d)                        # down across the body's line
        down = -down if down[2] > 0 else down
        p = c + side * r * 0.86 + down * r * 0.32
        br = 0.015 + 0.006 * i / (SEGS - 1)
        k.add(f"bead{i}", ell(p, (br, br, br), sub=3), PINK, bone_of(c), fuse=False, mirror=True)


def nose():
    k.add("nose", ell(H + (0, -0.086, -0.012), (0.05, 0.042, 0.04), rot=(-12, 0, 0), sub=4), PINK, "head",
          fuse=False)


def horn():
    d = unit(0, 0.28, 1)
    base = H + d * (HR[2] * 0.72)
    k.add("horn", lathe(base, d, [(0, 0), (0.038, 0.0), (0.03, 0.04), (0.016, 0.068), (0.0, 0.104)]), STING, "head",
          fuse=False, surface="shell")


def stinger():
    c, d, r, s = CENTRES[0]
    base = c + np.array([0, 0.012, 0.0])
    pts = [base, base + (0, 0.03, 0.04), base + (0, 0.042, 0.09), base + (0, 0.038, 0.14)]
    k.add("stinger", outward(tube(pts, [0.034, 0.026, 0.014, 0.0012])), STING, "tail", fuse=False, surface="shell")


# --- the eyes: small black ovals with a white glint, up and out from the nose ---------------------------------------
CK, CW, CL = srgb(INK), np.array([1.0, 1.0, 1.0]), srgb(TAN)
def eye_open(u, v):
    c = np.tile(CK, (len(u), 1))
    c[((u + 0.25) / 0.34) ** 2 + ((v - 0.3) / 0.34) ** 2 < 1] = CW
    return c


def eye_frame(side, d=(0.58, -0.76, 0.3)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


EYE_HALF = (0.0145, 0.019)
def eyes(state):
    look = eye_look(state, eye_open, CL, CK, line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, n, EYE_HALF, col, eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    add_body()
    # no painted seams: the creases between the fused segments read as them (paint over them came out ragged)
    beads()
    nose()
    horn()
    stinger()
    k.build(voxel=0.005, smooth=2)
    k.eye_states(eyes)
    k.export(out)
