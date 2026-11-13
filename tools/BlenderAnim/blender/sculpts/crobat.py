# Crobat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/crobat.py -- <the export's Crobat.glb> <out.glb>
# Grown from Golbat's script: the same way of making wings (bones and membrane panels) and eyes (almond patches cut
# by the brow, a pupil of its own); Crobat is a small round purple body with four wings, the great upper pair teal
# within, the lower pair (its legs once) purple; wedge ears out to the sides, angry yellow eyes with red pupils, a
# clenched grin of white teeth and two tiny clawed feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, LOWSKIN, TEAL = "#9A5CC8", "#B07ED8", "#5DB4C4"
WHITE, INKH = "#F6F4F2", "#2A1430"

# The body: a round ball, a little fuller above where the head is
C = np.array([0.0, 0.0, 0.345])
k.add("body", ell(C, (0.12, 0.112, 0.122)), PURPLE, "body")
k.add("crown", ell((0, 0.004, 0.41), (0.1, 0.09, 0.065)), PURPLE, "head")

# Wedge ears out to the sides and a little up, wide at the root
k.add("ear", blade([(0.06, 0.015, 0.43), (0.12, 0.03, 0.47), (0.17, 0.045, 0.505), (0.205, 0.055, 0.53)],
                   [0.05, 0.035, 0.018, 0.004], (0, 1, 0.2), 0.45), PURPLE, "ear{s}", mirror=True)

# Two tiny feet under the body, three little claws each
k.add("foot", tube([(0.035, 0.0, 0.24), (0.04, -0.005, 0.2)], [0.02, 0.016]), PURPLE, "body", fuse=False, mirror=True)
for i, a in enumerate((-1, 0, 1)):
    base = np.array((0.04 + 0.01 * a, -0.008, 0.195))
    tip = base + np.array((0.012 * a, -0.012, -0.03))
    k.add(f"toe{i}", tube([base, tip], [0.008, 0.002]), PURPLE, "body", fuse=False, mirror=True)


def membrane(name, S, W, TIPS, ROOT, bone, skin, bones, rb, rf, scallop=0.2):
    """A bat's wing: bones from the shoulder to the wrist and fingers out from it, a membrane between."""
    k.add(f"{name}arm", tube([S, (S + W) / 2, W], rb), bones, bone, fuse=False, mirror=True)
    for i, t in enumerate(TIPS):
        k.add(f"{name}finger{i}", tube([W, W + (t - W) * 0.5, t], rf), bones, bone, fuse=False, mirror=True)
    def panel(a, b, sc):
        def at(u, v):
            u = 0.015 + 0.97 * u
            e = a[None, None] + (b - a)[None, None] * u[..., None]
            e = e - (e - W) * (sc * np.sin(np.pi * u))[..., None]
            v = 0.04 + 0.96 * v
            return W + (e - W) * v[..., None]
        return at
    rim = [S, ROOT] + TIPS[::-1]
    for i in range(len(rim) - 1):
        k.add(f"{name}skin{i}", sheet(panel(rim[i], rim[i + 1], 0.0 if i == 0 else scallop), 22, 16, 0.012), skin,
              bone, fuse=False, mirror=True)


# The great upper wings: a thick purple leading edge from the shoulder to a high wrist and out to a far tip, a finger
# down to the trailing edge, teal between
S = np.array([0.095, 0.01, 0.41])
W = np.array([0.30, 0.04, 0.6])
TIPS = [np.array([0.76, 0.07, 0.62]), np.array([0.56, 0.06, 0.4])]
ROOT = np.array([0.09, 0.02, 0.33])
membrane("up", S, W, TIPS, ROOT, "wing{s}", TEAL, PURPLE, [0.032, 0.028, 0.024], [0.02, 0.014, 0.004], 0.22)
# two little claws at the wrist
for i, (d, L) in enumerate((((0.25, 0, 1), 0.045), ((0.75, 0.05, 0.7), 0.035))):
    d = unit(*d)
    k.add(f"claw{i}", tube([W, W + d * L], [0.014, 0.002]), PURPLE, "wing{s}", fuse=False, mirror=True)

# The smaller lower wings, the legs it once had: out and down from below the body, purple through and through
S2 = np.array([0.07, 0.012, 0.29])
W2 = np.array([0.22, 0.03, 0.27])
TIPS2 = [np.array([0.42, 0.045, 0.21]), np.array([0.34, 0.045, 0.06])]
ROOT2 = np.array([0.06, 0.02, 0.22])
membrane("low", S2, W2, TIPS2, ROOT2, "lowerWing{s}", LOWSKIN, PURPLE, [0.026, 0.023, 0.02], [0.016, 0.012, 0.004],
         0.2)

# The grin: clenched white teeth, two rows met at a zigzag, a fang at each upper corner, dark round its rim
CT, CI = srgb(WHITE), srgb(INKH)
def grin(u, v):
    c = np.tile(CT, (len(u), 1))
    top = 0.6 - 0.15 * u * u
    bot = -0.75 + 0.25 * u * u
    c[(v > top - 0.07) | (v < bot + 0.07)] = CI
    zig = 0.1 * (2 * np.abs(2 * ((u * 2.5) - np.floor(u * 2.5 + 0.5))) - 1)
    c[np.abs(v - (-0.08 + zig)) < 0.05] = CI
    for x in (-0.4, 0.0, 0.4):
        c[(np.abs(u - x) < 0.025) & (v > -0.08)] = CI
        c[(np.abs(u - x - 0.2) < 0.025) & (v < -0.08) & (np.abs(u) < 0.8)] = CI
    # the fangs hang below the upper row's line at each end
    for x in (-0.7, 0.7):
        f = (np.abs(u - x) < 0.13 * np.clip((v + 0.35) / 0.4, 0, 1)) & (v < -0.05) & (v > -0.35)
        c[f] = CT
    return c
# two little fangs, loose, standing over the grin's upper corners as Golbat's and Zubat's do
k.add("fang", tube([(0.044, -0.099, 0.328), (0.045, -0.106, 0.316), (0.046, -0.109, 0.304)], [0.008, 0.005, 0.0012]),
      WHITE, "body", fuse=False, mirror=True, surface="shell")
mn = unit(0, -1, -0.15)
k.decal((0, -0.1, 0.312), mn, (0.068, 0.027), grin, eps=0.004, rings=14, segs=60,
        outline=lambda th: (1 - 0.35 * abs(math.cos(th)) ** 3))

# The eyes: angry yellow almonds under a purple brow that slants down toward the middle, a red pupil toward the
# inner corner. u runs outward, v up.
EYE, PUP, PUPD, LID, INK = srgb("#F8C828"), srgb("#D8283A"), srgb("#701020"), srgb(PURPLE), srgb("#1E0E26")
def eye_top(u):
    return 0.3 + 0.42 * u
def eye_bottom(u):
    return eye_top(u) - 0.95 * (1 - u * u) ** 0.7
ALMOND = [(u, eye_top(u)) for u in np.linspace(1, -1, 9)] + [(u, eye_bottom(u)) for u in np.linspace(-0.92, 0.92, 13)]
def white(u, v):
    c = np.tile(EYE, (len(u), 1))
    c[v > eye_top(u) - 0.14] = INK
    return c
de = unit(0.55, -0.8, 0.22)
EH = 0.034
E = np.array([0.064, -0.096, 0.402])
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
PU, PV, PA, PB = -0.22, -0.22, 0.011 / EH, 0.013 / EH      # the pupil's middle and half sizes in the eye's units
def pupil_look(state):
    """The pupil, cut by the brow's ink line (and the Fierce lid) where it reaches up into it."""
    def look(a, b):
        u, v = PU + a * PA, PV + b * PB
        c = np.tile(PUP, (len(a), 1))
        c[a * a + b * b < 0.3] = PUPD
        if state == "Fierce":
            edge = 0.2 + 0.5 * u
            c[v > edge - 0.13] = INK
            c[v > edge + 0.13] = LID
        c[v > eye_top(u) - 0.14] = INK
        return c
    return look
def eyes(state):
    for side in (1, -1):
        m = np.array([side, 1, 1.0])
        c, n = E * m, de * m
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        k.decal(c, n, (EH, EH), flip(eye_look(state, white, LID, INK, line=0.13)), eps=0.005, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (PU * EH) + up * (PV * EH)
            k.decal(pc, n, (PA * EH, PB * EH), flip(pupil_look(state)), eps=0.008, rings=8, segs=28)

k.build(voxel=0.006, smooth=4)
k.eye_states(eyes)
k.export(out)
