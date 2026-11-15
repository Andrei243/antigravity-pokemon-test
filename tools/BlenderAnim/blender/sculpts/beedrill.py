# Beedrill sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/beedrill.py -- <the export's Beedrill.glb> <out.glb>
#
# Weedle's and Kakuna's line grown into a wasp. From the references: a round yellow head with two great red compound
# eyes (a pink glint, a dark rim), two short kinked grey-blue antennae; a small yellow thorax on a thin neck; a narrow
# waist to a big yellow abdomen hanging down and back, two black bands round it and a yellow stinger at its tip.
# Thin grey-blue arms, each ending in a great white cone of a stinger; one pair of thin grey-blue legs; two pairs of
# pale wings with grey veins behind. The art's legs hang long and its arms are flung wide; here the legs are tucked
# and the stingers carried forward and down (the brief's rules for the game's cameras).
# Skeleton: body (0, 0, 0.485), head (0, -0.02, 0.555), ears (+-0.02, -0.03, 0.665), arms (+-0.06, -0.02, 0.515),
# wings (+-0.04, 0.05, 0.525), legs (+-0.04, -0.02, 0.455), hind (+-0.04, 0.04, 0.425: left empty, the pictures show
# one pair of legs beside the arms).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, slab, ring_band, outward, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, BLACK, LIMB, STING = "#F6CA36", "#2E2C36", "#6E7A9E", "#E8ECF2"
WING, VEIN, RED, PINK, RIM = "#E6EBF2", "#A6AEBE", "#E0303E", "#FF9CAA", "#5A141E"

# --- the frame -------------------------------------------------------------------------------------------------------
H, HR = np.array([0.0, -0.035, 0.632]), np.array([0.088, 0.086, 0.084])     # the head
TH_C, TH_R = np.array([0.0, 0.0, 0.5]), (0.06, 0.06, 0.058)                 # the thorax
AB_TILT = 21.0                                                               # the abdomen hangs down and back
AB_TOP = np.array([0.0, 0.03, 0.44])
AB_L, AB_R = 0.145, 0.102                                                    # its half length and its radius
AB_AX = np.array([0.0, math.sin(math.radians(AB_TILT)), -math.cos(math.radians(AB_TILT))])
AB_C = AB_TOP + AB_AX * AB_L
SHOULDER = np.array([0.055, -0.02, 0.515])
ELBOW = np.array([0.125, -0.05, 0.47])
WRIST = np.array([0.15, -0.105, 0.445])
ST_DIR = unit(0.46, -0.85, -0.2)                                              # the arm's stinger: forward and down


def ab_radius(s):
    """The abdomen's radius at a distance s along its axis from its top."""
    t = np.clip((s - AB_L) / AB_L, -1, 1)
    return AB_R * np.sqrt(1 - t * t)


def add_body():
    k.add("head", ell(H, HR), YELLOW, "head")
    k.add("neck", tube([(0, -0.012, 0.53), (0, -0.025, 0.57)], [0.03, 0.03]), YELLOW, "head")
    k.add("thorax", ell(TH_C, TH_R), YELLOW, "body")
    k.add("waist", tube([(0, 0.01, 0.47), AB_TOP + AB_AX * 0.03], [0.026, 0.03]), YELLOW, "body")
    k.add("abdomen", ell(AB_C, (AB_R, AB_R, AB_L), rot=(AB_TILT, 0, 0), sub=5), YELLOW, "body")


def stripes():
    """Two black bands round the abdomen, loose rings so their edges stay crisp."""
    for i, (s0, w) in enumerate(((0.12, 0.045), (0.19, 0.04))):
        k.add(f"stripe{i}", ring_band(AB_TOP + AB_AX * s0, AB_AX, lambda v, s0=s0, w=w: ab_radius(s0 + (v - 0.5) * w) + 0.004,
                                      w, thick=0.006), BLACK, "body", fuse=False)


def tail_stinger():
    base = AB_TOP + AB_AX * (2 * AB_L - 0.03)
    k.add("tailsting", lathe(base, AB_AX, [(0, 0), (0.032, 0.0), (0.026, 0.03), (0.012, 0.062), (0.0, 0.088)]), YELLOW,
          "body", fuse=False, surface="shell")


def arms():
    k.add("upper", tube([SHOULDER, (SHOULDER + ELBOW) / 2 + (0, 0, 0.004), ELBOW], [0.016, 0.013, 0.013]), LIMB,
          "arm{s}", fuse=False, mirror=True)
    k.add("fore", tube([ELBOW, (ELBOW + WRIST) / 2, WRIST], [0.013, 0.013, 0.016]), LIMB, "arm{s}", fuse=False,
          mirror=True)
    # the great stinger: a round cuff over the wrist, a long white cone forward and down
    k.add("lance", lathe(WRIST - ST_DIR * 0.012, ST_DIR,
                         [(0, -0.02), (0.03, -0.016), (0.046, 0.006), (0.044, 0.04), (0.032, 0.13), (0.015, 0.215),
                          (0.0, 0.29)]), STING, "arm{s}", fuse=False, mirror=True, surface="shell")


def legs():
    pts = [(0.035, -0.02, 0.46), (0.08, -0.045, 0.41), (0.088, -0.05, 0.37), (0.078, -0.03, 0.33)]
    k.add("leg", tube(pts, [0.012, 0.011, 0.009, 0.004]), LIMB, "leg{s}", fuse=False, mirror=True)


def antennae():
    pts = [(0.022, -0.07, 0.69), (0.034, -0.06, 0.745), (0.042, -0.063, 0.775), (0.075, -0.1, 0.785)]
    k.add("antenna", tube(pts, [0.0075, 0.007, 0.0065, 0.0055]), LIMB, "ear{s}", fuse=False, mirror=True)


# --- the wings: two pairs of pale plates with grey veins, raised in a V and swept apart --------------------------------
def wing_outline(L, W, n=26):
    t = np.linspace(0, 1, n)
    w = W * np.sqrt(np.clip(1 - ((t - 0.55) / 0.55) ** 2, 0, 1)) * (0.45 + 0.55 * t)
    a = 0.004 + t * L
    return np.vstack([np.stack([a, np.maximum(w, 0.003)], 1), np.stack([a, -np.maximum(w, 0.003)], 1)[::-1]])


def wing_axes(sweep, lift, twist):
    """On +X: outward raised by lift and swept by sweep (degrees, + forward); the width along the body tilted."""
    sw, li = math.radians(sweep), math.radians(lift)
    ex = np.array([math.cos(li) * math.cos(sw), -math.cos(li) * math.sin(sw), math.sin(li)])
    ez = np.array([0.0, 1.0, math.tan(math.radians(twist))])
    ez = ez - ex * np.dot(ez, ex)
    return ex, ez / np.linalg.norm(ez)


def wing(name, root, L, W, sweep, lift, twist):
    ex, ez = wing_axes(sweep, lift, twist)
    o = np.array(root, float)
    k.add(name, slab(wing_outline(L, W), o, ex, ez, 0.012), WING, "wing{s}", fuse=False, mirror=True)
    # veins: one along the wing, a few across it, thicker than the plate so they show on both faces
    veins = [[(0.03 * L, -0.01 * W), (0.9 * L, 0.04 * W), (0.9 * L, 0.15 * W), (0.03 * L, 0.09 * W)]]
    for t in (0.42, 0.74):
        w = W * math.sqrt(max(0.0, 1 - ((t - 0.55) / 0.55) ** 2)) * (0.45 + 0.55 * t) * 0.92
        veins.append([(t * L, -w), (t * L + 0.008, -w), (t * L + 0.008, w), (t * L, w)])
    for i, v in enumerate(veins):
        k.add(f"{name}vein{i}", slab(v, o, ex, ez, 0.015), VEIN, "wing{s}", fuse=False, mirror=True)


def wings():
    wing("wingU", (0.035, 0.05, 0.54), 0.25, 0.075, -8, 32, 50)
    wing("wingD", (0.035, 0.08, 0.5), 0.19, 0.058, -42, 28, -30)


# --- the eyes: great red ovals turned out from the front, a pink glint, a dark rim -------------------------------------
CR, CP, CRIM, CW = srgb(RED), srgb(PINK), srgb(RIM), np.array([1.0, 1.0, 1.0])
def eye_open(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.22) / 0.4) ** 2 + ((v - 0.28) / 0.3) ** 2 < 1] = CP
    c[((u + 0.3) / 0.14) ** 2 + ((v - 0.36) / 0.12) ** 2 < 1] = CW
    c[u * u + v * v > 0.72] = CRIM
    return c


def eye_frame(side, d=(0.66, -0.7, 0.16)):
    de = unit(side * d[0], d[1], d[2])
    p = H + de / math.sqrt(((de / HR) ** 2).sum())
    n = (p - H) / HR ** 2
    return p, n / np.linalg.norm(n)


EYE_HALF = (0.036, 0.046)
def eyes(state):
    # the eyes are the face's mark: a closed eye keeps their red as its lid
    look = eye_look(state, eye_open, CR, CRIM, line=0.17)
    for side in (1, -1):
        c, n = eye_frame(side)
        col = look if side == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(c, n, EYE_HALF, col, up=(side * 0.35, 0, 1), eps=0.003, rings=20, segs=56)


if __name__ == "__main__":
    add_body()
    stripes()
    tail_stinger()
    arms()
    legs()
    antennae()
    wings()
    k.build(voxel=0.005, smooth=4)
    k.eye_states(eyes)
    k.export(out)
