# Golbat sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/golbat.py -- <the export's Golbat.glb> <out.glb>
# Grown from Zubat's script: the same blue and lilac, the same carved mouth with loose fangs and wings of bones and
# membrane; Golbat is nearly all mouth, with small slanted eyes over it and short legs.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, PURPLE = "#2C98E8", "#B498E8"
MOUTH, WHITE = "#221C28", "#F6F4F2"

# The body: a tall bean, fuller low at the front where the jaw is
C = np.array([0.0, 0.0, 0.565])
k.add("body", ell(C, (0.15, 0.14, 0.205)), BLUE, "body")
k.add("jaw", ell((0, -0.025, 0.475), (0.135, 0.13, 0.125)), BLUE, "body")
k.add("crown", ell((0, 0.0, 0.67), (0.12, 0.11, 0.08)), BLUE, "head")

# Short pointed ears on top, a little outward
k.add("ear", blade([(0.07, 0.0, 0.72), (0.095, 0.004, 0.775), (0.118, 0.008, 0.815)],
                   [0.042, 0.026, 0.006], (0, 1, 0), 0.5), BLUE, "ear{s}", mirror=True)

# The mouth: a tall hollow carved down most of the front, near black within
k.add("mouth", ell((0, -0.13, 0.53), (0.096, 0.075, 0.152)), MOUTH, "body", carve=True)

# Four fangs, loose at the rim: two long ones hanging from the top corners, two short ones rising from the foot
k.add("fang", tube([(0.052, -0.108, 0.665), (0.055, -0.125, 0.638), (0.056, -0.132, 0.612)], [0.014, 0.01, 0.0018]),
      WHITE, "head", fuse=False, mirror=True, surface="shell")
k.add("lowfang", tube([(0.045, -0.13, 0.41), (0.047, -0.14, 0.428), (0.048, -0.145, 0.447)], [0.012, 0.008, 0.0015]),
      WHITE, "body", fuse=False, mirror=True, surface="shell")

# Short legs, bent at the knee, a little foot of three toes each
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", tube(np.array([(0.075, 0.0, 0.42), (0.085, -0.01, 0.37)]) * m, [0.036, 0.03]), BLUE,
          f"leg{side}")
    k.add(f"leg{side}", tube(np.array([(0.08, -0.005, 0.39), (0.1, -0.045, 0.31), (0.095, 0.0, 0.235)]) * m,
                             [0.028, 0.025, 0.022]), BLUE, f"leg{side}", fuse=False)
    k.add(f"foot{side}", ell(np.array((0.095, -0.012, 0.222)) * m, (0.03, 0.036, 0.018)), BLUE, f"leg{side}",
          fuse=False)
    for i, a in enumerate((-0.5, 0.0, 0.5)):
        base = np.array((0.095 + 0.012 * a, -0.03, 0.218))
        tip = base + np.array((0.03 * a, -0.03, -0.006))
        k.add(f"toe{side}{i}", tube(np.array([base, tip]) * m, [0.011, 0.006]), BLUE, f"leg{side}", fuse=False)

# The wings: blue bones from the shoulder up to a high wrist over the head and three fingers out from it, a lilac
# membrane between, its free edges scalloped between the fingertips
S = np.array([0.12, 0.0, 0.64])          # the shoulder
W = np.array([0.30, 0.03, 0.94])         # the wrist
TIPS = [np.array([0.84, 0.07, 0.86]), np.array([0.76, 0.07, 0.55]), np.array([0.48, 0.05, 0.38])]
ROOT = np.array([0.11, 0.01, 0.49])      # where the membrane's lower edge meets the body
k.add("arm", tube([S, (S + W) / 2 + [0.01, 0, 0.0], W], [0.03, 0.026, 0.022]), BLUE, "wing{s}", fuse=False, mirror=True)
for i, t in enumerate(TIPS):
    k.add(f"finger{i}", tube([W, W + (t - W) * 0.5, t], [0.016, 0.012, 0.004]), BLUE, "wing{s}", fuse=False,
          mirror=True)
def panel(a, b, scallop):
    def at(u, v):
        e = a[None, None] + (b - a)[None, None] * u[..., None]
        e = e - (e - W) * (scallop * np.sin(np.pi * u))[..., None]
        v = 0.04 + 0.96 * v
        return W + (e - W) * v[..., None]
    return at
rim = [S, ROOT] + TIPS[::-1]
for i in range(len(rim) - 1):
    scallop = 0.0 if i == 0 else 0.2
    k.add(f"skin{i}", sheet(panel(rim[i], rim[i + 1], scallop), 22, 16, 0.012), PURPLE, "wing{s}", fuse=False,
          mirror=True)

k.build(voxel=0.006, smooth=4)

# The eyes: small white almonds over the mouth, their tops slanting down toward the middle under a blue brow, a small
# black pupil toward the inner corner. u runs outward, v up.
EW, PUP, LID, INK = srgb("#F6F4F8"), srgb("#141018"), srgb(BLUE), srgb("#0E1A2C")
ALMOND = [(1.0, 0.55), (0.45, 0.3), (-0.2, 0.0), (-0.95, -0.35), (-0.55, -0.6), (0.2, -0.62), (0.75, -0.35),
          (1.0, 0.0)]
def white(u, v):
    return np.tile(EW, (len(u), 1))
de = unit(0.42, -0.8, 0.42)
EH = 0.04
def frame(n):
    right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
    return right, np.cross(n, right)
def eyes(state):
    for side in (1, -1):
        n = de * np.array([side, 1, 1.0])
        c = np.array([0.058 * side, -0.1, 0.705])
        right, up = frame(n)
        flip = (lambda f: f) if side == 1 else (lambda f: (lambda u, v: f(-u, v)))
        outline = polygon([(side * u, v) for u, v in (ALMOND if side == 1 else ALMOND[::-1])])
        k.decal(c, n, (EH, EH), flip(eye_look(state, white, LID, INK, line=0.12)), eps=0.005, outline=outline,
                rings=20, segs=56)
        if state in ("Open", "Fierce"):
            pc = c + right * side * (-0.25 * EH) + up * (-0.2 * EH)
            k.decal(pc, n, (0.007, 0.009), PUP, eps=0.008, rings=5, segs=20)

k.eye_states(eyes)
k.export(out)
