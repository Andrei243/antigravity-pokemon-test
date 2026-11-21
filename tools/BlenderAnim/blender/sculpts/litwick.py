# Litwick sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/litwick.py -- <the export's Litwick.glb> <out.glb>
#
# The design, from the references: a little candle, a gumdrop wider at its round top shoulders than below, standing
# in a puddle of its own wax. Its upper part is white wax whose lower edge runs round it with a soft scallop and on
# its +X front side hangs down in a lock, under whose inner edge sits its one yellow eye (a tall gold oval, the wax
# hides the other); the lower part is a pale blue-lavender. A small smile in the middle, low; two little round nubs of
# arms at the lower sides. On its flat top a lavender disc, and on that a teardrop flame, purple above, blue below,
# with a pale heart. Written to be grown from: Lampent and Chandelure keep its flame and its eye.
# The skeleton: body (0, 0, 0.12), head (0, 0, 0.22) at the flame's foot, armR/armL at (+-0.06, -0.01, 0.1).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, inside, unit, eye_look, boxy, lumpy, args

# The line's colours (Lampent and Chandelure take their flames and eyes from these)
WAX, LOWER, DISC, INK = "#F3F3F9", "#BCC4E8", "#ADB2E6", "#2E2638"
FLAME, FLAMEFOOT, FLAMEHEART = "#7A6CCB", "#6E9BE0", "#C9D3F6"
GLOW, GLOWFOOT, GLOWHEART = "#6A5AD0", "#5A8AE6", "#B8C6FF"
GOLD, GOLDDEEP, EYERIM = "#F0C93A", "#D7A422", "#3A2E1E"
MOUTH = "#5C5480"


def teardrop(V):
    """The unit sphere made a flame's tongue: round below its widest, a third of the way up, pointed on top."""
    V = V.copy()
    t = np.clip(V[:, 2], -1, 1)
    w = np.sqrt(np.clip(1 + t, 0, None)) * np.clip(1 - t, 0, None) ** 1.2 / 1.16
    V[:, 0] *= w; V[:, 1] *= w
    return V


def flame(k, base, height, width, bone, depth=0.7, heart=True, rot=(0, 0, 0)):
    """
    A teardrop flame standing upright on base: a purple tongue, a smaller blue tongue in its lower part (a little
    thicker, so it shows on both faces with a pointed edge into the purple) and a pale heart showing through both,
    each a loose glowing piece (Lampent's and Chandelure's cups are the same).
    """
    base = np.array(base, float)
    hz = height / 2
    k.add("flame", ell(base + (0, 0, hz), (width / 2, width / 2 * depth, hz), rot=rot, deform=teardrop),
          FLAME, bone, fuse=False, glow=GLOW, surface="skin")
    fz = height * 0.6 / 2
    k.add("flamefoot", ell(base + (0, 0, fz + height * 0.004), (width / 2 * 0.8, width / 2 * depth * 1.1, fz),
                           rot=rot, deform=teardrop), FLAMEFOOT, bone, fuse=False, glow=GLOWFOOT, surface="skin")
    if heart:
        k.add("flameheart", ell(base + (0, 0, height * 0.4), (width * 0.17, width / 2 * depth * 1.2, height * 0.24),
                                rot=rot, deform=teardrop), FLAMEHEART, bone, fuse=False, glow=GLOWHEART, surface="skin")


def gold_eye(u, v):
    """A tall gold oval in a dark rim a sixth of its size, its lower part a deeper gold."""
    r = np.hypot(u, v)
    c = np.tile(srgb(GOLD), (len(u), 1))
    c[v < -0.35] = srgb(GOLDDEEP)
    c[r > 0.8] = srgb(EYERIM)                 # between two rings of the patch
    return c


# --- the frame -------------------------------------------------------------------------------------------------------
CAP = np.array([0.0, 0.0, 0.15]); CAPR = np.array([0.118, 0.108, 0.066])      # the white shoulders, flat on top
LOW = np.array([0.0, 0.0, 0.08]); LOWR = np.array([0.1, 0.094, 0.075])        # the lower candle
TOP = 0.214                                                                   # the flat top the flame stands on
EYE_A, EYE_Z, EYE_HALF = 0.4, 0.09, (0.0135, 0.02)                            # azimuth (+X positive), height, half size
LOCK_A, LOCK_W, LOCK_DIP = 0.82, 0.4, 0.036                                 # the lock of wax over the eye's outer side


def edge(a):
    """The white wax's lower edge round the candle (a: azimuth from the front, +X positive)."""
    base = 0.12 + 0.005 * np.cos(5 * a)
    lock = LOCK_DIP * np.sqrt(np.clip(1 - ((a - LOCK_A) / LOCK_W) ** 2, 0, 1))
    return base - lock


def candle(k):
    k.add("cap", ell(CAP, CAPR, deform=boxy(2.6)), WAX, "body")
    k.add("low", ell(LOW, LOWR, deform=boxy(2.3)), LOWER, "body")
    # the lock, a bulge of wax hanging over the +X front
    la = LOCK_A
    k.add("lock", ell((0.098 * math.sin(la), -0.088 * math.cos(la), 0.118), (0.034, 0.026, 0.042)), WAX, "body")
    # the puddle it stands in, its rim in soft lobes
    k.add("puddle", ell((0, 0, 0.017), (0.108, 0.1, 0.018), deform=lumpy(5, 1, 0.07)), LOWER, "body")
    # two little nubs of arms at the lower sides
    k.add("nub", ell((0.1, -0.03, 0.062), (0.024, 0.026, 0.03)), LOWER, "arm{s}", mirror=True)
    # the white's edge, crisp: white above it, the lower colour below, only on the candle itself
    def az(P):
        return np.arctan2(P[:, 0], -P[:, 1])
    body = lambda P: (P[:, 2] < TOP + 0.01) * (P[:, 2] > 0.03)
    k.paint(lambda P: inside(edge(az(P)) - P[:, 2], 0.002) * body(P), WAX)
    k.paint(lambda P: inside(P[:, 2] - edge(az(P)), 0.002) * body(P), LOWER)
    # the lavender disc on the flat top, round the flame's foot
    k.decal((0, -0.004, TOP), (0, 0, 1), (0.072, 0.066), DISC, up=(0, -1, 0), eps=0.003, rings=12, segs=48)
    # a small smile, low in the middle
    def smile(u, v):
        c = np.tile(srgb(LOWER), (len(u), 1))
        line = -0.3 + 0.75 * u * u
        c[(np.abs(v - line) < 0.22) & (np.abs(u) < 0.9)] = srgb(MOUTH)
        return c
    md = unit(-0.08, -1, -0.05)
    k.decal((-0.008, -0.094, 0.058), md, (0.017, 0.01), smile, eps=0.003, rings=10, segs=48)


def eye_at(a=EYE_A, z=EYE_Z):
    n = unit(math.sin(a), -math.cos(a), 0.06)
    return np.array([0.1 * math.sin(a), -0.094 * math.cos(a), z]), n


def eyes(k, state, lid=LOWER):
    c, n = eye_at()
    k.decal(c, n, EYE_HALF, eye_look(state, gold_eye, srgb(lid), srgb(EYERIM), line=0.17), eps=0.003, rings=20,
            segs=56)


def litwick(src, out):
    k = Kit(src, coat="fur")
    candle(k)
    flame(k, (0, 0, TOP - 0.012), 0.16, 0.1, "head")
    k.build(voxel=0.0038, smooth=3, band=0.005)
    k.eye_states(lambda s: eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    litwick(*args())
