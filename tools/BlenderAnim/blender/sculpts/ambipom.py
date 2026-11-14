# Ambipom sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ambipom.py -- <the export's Ambipom.glb> <out.glb>
#
# Grown from Aipom's script: the same purple and cream, the face painted on a fused head with a muzzle standing a little
# forward, the grin carved with loose teeth seated in it, the ears' linings laid as patches. From the references: a big
# round head on a small body; big round ears out at the sides lined red-orange; a cowlick of two strands rising from
# the crown and curling outward; a cream face cut straight across under a purple fringe just over the eyes, tall white
# eyes ringed black with a round black pupil, a small purple "^" of a nose, a wide grin of clenched teeth. Thin arms
# held out a little, ending in points; a cream belly; little tufts at the hips; short legs on purple feet. And the two
# long tails: out of the back, round the sides and up past the shoulders to a ring of purple puffs and a great cream
# hand, as big as the head, raised above it with three thick fingers spread outward, each tipped red.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, CREAM, MOUTH, WHITE, INK = "#B286CF", "#F5DFA0", "#5A2A38", "#FBFAF4", "#2A1E2C"
RED = "#E8604A"
H = np.array([0, -0.005, 0.60])            # the head's centre
HR = (0.17, 0.15, 0.16)
E_ = 0.0035

# --- head, body, legs -------------------------------------------------------------------------------------------
k.add("head", ell(H, HR), PURPLE, "head")
k.add("muzzle", ell((0, -0.055, 0.555), (0.145, 0.1, 0.105)), PURPLE, "head")
k.add("neck", ell((0, 0.0, 0.45), (0.07, 0.065, 0.06)), PURPLE, "body")
k.add("body", ell((0, 0.005, 0.34), (0.098, 0.082, 0.125)), PURPLE, "body")
k.add("leg", tube([(0.055, 0.0, 0.27), (0.075, 0.0, 0.17), (0.1, -0.01, 0.08), (0.115, -0.02, 0.035)],
                  [0.04, 0.032, 0.027, 0.026]), PURPLE, "leg{s}", mirror=True)
k.add("foot", ell((0.122, -0.045, 0.026), (0.046, 0.075, 0.028)), PURPLE, "leg{s}", mirror=True)
# little tufts of fur at the hips, out and back
for i, (dz, dy) in enumerate(((0.02, 0.0), (-0.01, 0.025), (0.005, -0.02))):
    b = np.array([0.075, 0.025 + dy, 0.285 + dz])
    t = b + np.array([0.06, 0.03 + dy, -0.025 + dz * 0.5])
    k.add(f"tuft{i}", blade([b, (b + t) / 2, t], [0.022, 0.014, 0.003], (0, 1, 0), 0.6), PURPLE, "body", mirror=True)

# thin arms held out a little and down, ending in points
k.add("arm", tube([(0.075, -0.02, 0.42), (0.15, -0.035, 0.392), (0.22, -0.05, 0.366), (0.275, -0.06, 0.352)],
                  [0.026, 0.021, 0.015, 0.006]), PURPLE, "arm{s}", mirror=True)

# big round ears out at the sides, turned a little forward
k.add("ear", ell((0.225, 0.02, 0.6), (0.08, 0.026, 0.09), rot=(0, 0, 20)), PURPLE, "ear{s}", mirror=True)

# the cowlick: a short stem from the crown, splitting into two strands that rise and curl outward
k.add("stem", tube([(0, 0.0, 0.72), (0, 0.004, 0.8)], [0.024, 0.014]), PURPLE, "head")
k.add("strand", tube([(0.0, 0.004, 0.795), (0.03, 0.007, 0.84), (0.072, 0.01, 0.865), (0.108, 0.01, 0.855),
                      (0.125, 0.008, 0.825)], [0.013, 0.012, 0.011, 0.01, 0.009]), PURPLE, "head", mirror=True)

# --- the two tails and their hands -------------------------------------------------------------------------------
TP = [(0.03, 0.07, 0.25), (0.12, 0.12, 0.225), (0.26, 0.12, 0.26), (0.37, 0.09, 0.36), (0.42, 0.06, 0.5),
      (0.44, 0.04, 0.63), (0.45, 0.03, 0.7)]
k.add("tail", tube(TP, [0.026, 0.025, 0.024, 0.024, 0.024, 0.025, 0.028]), PURPLE, "tail", mirror=True)
W = np.array([0.45, 0.03, 0.725])           # the wrist: a ring of purple puffs
for i in range(7):
    a = 2 * math.pi * i / 7
    k.add(f"puff{i}", ell(W + np.array([0.048 * math.cos(a), 0.042 * math.sin(a), 0.006 * math.sin(3 * a)]),
                         (0.036, 0.034, 0.033)), PURPLE, "tail", mirror=True)
C = np.array([0.462, 0.0, 0.835])           # the palm: as big as the head
k.add("palm", ell(C, (0.14, 0.08, 0.122)), CREAM, "tail", mirror=True)
FINGERS = []
for i, a in enumerate((102, 64, 22)):
    d = unit(math.cos(math.radians(a)), -0.12, math.sin(math.radians(a)))
    FINGERS.append(d)
    k.add(f"finger{i}", tube([C + d * 0.07, C + d * 0.15, C + d * 0.215], [0.036, 0.037, 0.04]), CREAM, "tail",
          mirror=True)
    k.add(f"fball{i}", ell(C + d * 0.232, (0.044, 0.042, 0.044)), CREAM, "tail", mirror=True)


def tips(P):
    """The fingers' red ends, along each finger's own line (both hands, by |x|)."""
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    m = np.zeros(len(P))
    for d in FINGERS:
        w = Q - C
        t = w @ d
        r = np.linalg.norm(w - np.outer(t, d), axis=1)
        m = np.maximum(m, inside(0.19 - t, E_) * (r < 0.07) * (t < 0.3))
    return m
k.paint(tips, RED)

# --- the face -----------------------------------------------------------------------------------------------------
def fringe(x):
    """The purple fringe's edge: straight across just over the eyes, a little point in the middle."""
    return 0.668 - 0.016 * np.clip(1 - np.abs(x) / 0.03, 0, 1)
def face(P):
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    oval = (x / 0.152) ** 4 + ((z - 0.565) / 0.125) ** 4 - 1
    return inside(oval * 0.06, E_) * inside(z - fringe(x), E_) * (y < -0.03) * (z > 0.4)
k.paint(face, CREAM)
# the belly
k.paint(lambda P: inside(((P[:, 0] / (0.052 + 0.18 * np.clip(0.36 - P[:, 2], 0, 1))) ** 2
                          + ((P[:, 2] - 0.335) / 0.088) ** 2 - 1) * 0.05, E_) * (P[:, 1] < -0.02) * (P[:, 2] < 0.44),
        CREAM)

# the grin: a hollow carved along a smile, two rows of white teeth clenched inside its rim
SZ, SY = 1.1, 2.6          # the smile's rise and its sweep back round the muzzle, per x squared
def smile(V):
    V = V.copy()
    V[:, 2] += SZ * V[:, 0] ** 2
    V[:, 1] += SY * V[:, 0] ** 2
    return V
MZ, MY = 0.51, -0.15
MW, MH = 0.108, 0.024
k.add("mouth", ell((0, MY, MZ), (MW, 0.022, MH), deform=smile), MOUTH, "head", carve=True)
# two rows of broad teeth nearly touching, so the grin reads as one white band split by thin dark lines
NT = 7
for i in range(NT):
    t = (i - (NT - 1) / 2) / ((NT - 1) / 2)
    x = MW * 0.76 * t
    zc = MZ + SZ * x * x
    yc = MY + SY * x * x + 0.016
    tw = MW * 0.76 / ((NT - 1) / 2) * 0.44
    f = max(0.5, math.sqrt(1 - (x / MW) ** 2))     # the hollow narrows toward the corners
    for row, dz in (("u", 0.0094 * f), ("l", -0.0094 * f)):
        k.add(f"tooth{row}{i}", ell((x, yc, zc + dz), (tw, 0.008, 0.0082 * f), deform=boxy(6)), WHITE, "head",
              fuse=False, surface="shell")

# the ears' red-orange linings
k.decal((0.225, -0.006, 0.6), unit(0.34, -0.94, 0), (0.054, 0.064), RED, eps=0.003, rings=12, segs=44, mirror=True)


# the small purple "^" of a nose
def nose(u, v):
    c = np.tile(srgb(CREAM), (len(u), 1))
    c[(np.abs(v - (0.55 - 1.15 * np.abs(u))) < 0.3) & (np.abs(u) < 0.9)] = srgb(PURPLE)
    return c
k.decal((0, -0.156, 0.558), unit(0, -1, 0.1), (0.022, 0.017), nose, eps=0.004, rings=10, segs=40)

# --- the eyes: tall white ovals ringed black, a round black pupil with a glint ------------------------------------
CC, CK, CW = srgb(CREAM), srgb(INK), np.array([1.0, 1.0, 1.0])
def eye(u, v):
    """A white oval with a solid dark rim a fifth of its size, a big black pupil toward the nose, one glint."""
    r = u * u + v * v
    c = np.tile(CK, (len(u), 1))
    c[r < 0.64] = CW
    c[((u + 0.12) / 0.5) ** 2 + ((v + 0.04) / 0.56) ** 2 < 1] = CK
    c[((u + 0.24) / 0.17) ** 2 + ((v - 0.18) / 0.17) ** 2 < 1] = CW
    return c
de = unit(0.36, -0.93, 0.06)
E = np.array([0.064, -0.142, 0.612])
def eyes(state):
    k.decal(E, de, (0.036, 0.049), eye_look(state, eye, CC, CK, line=0.2), eps=0.006, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0068)
k.eye_states(eyes)
k.export(out)
