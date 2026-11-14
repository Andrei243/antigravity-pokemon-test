# Medicham sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/medicham.py -- <the export's Medicham.glb> <out.glb>
# Grown from meditite.py: the same pale grey-white, the same round white eyes with a large black pupil cut by a lid
# sloping down to the nose, so that it frowns.
#
# The design, from the references: a slim meditating fighter; a pink cap over the top of a pale head, its edge
# straight across just over the eyes, a round pink lobe hanging out at each side, three yellow dots on its front (two
# above, one between them below) and a pink bulb on a short stalk standing up out of its top; round white eyes under
# heavy dark lids; small pink pouting lips; a thin neck and a thin pale body; long thin arms; great baggy pink trousers,
# a ball each leg, wider than everything else, a yellow spot low on each outer side and a yellow rim crossing over at
# the waist like a wrap; thin pale ankles drawn to points under them.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PALE, PINK, YELLOW, LIPS, MOUTH, INK = "#DCDDE5", "#E8457E", "#F4D27A", "#F06E98", "#9E2450", "#1A1A22"

# The head: a pale face with a flattened front, the cap a little wider over its top
HC = np.array([0, 0.004, 0.758])
# one round head (two shapes running along one another remesh rough where their colours meet), its face flattened
def capped(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.9 + 0.12 * smoothstep(0.3, 0.75, t)       # the cap a little wider over the top than the face
    return V
k.add("skull", ell(HC, (0.108, 0.1, 0.118), deform=capped), PALE, "head")
k.add("face", ell((0, -0.034, 0.73), (0.086, 0.066, 0.082)), PALE, "head")
# the round lobes hanging out at the sides, drooping a little
k.add("lobe", ell((0.15, 0.008, 0.762), (0.062, 0.048, 0.045), rot=(0, 18, 0)), PINK, "head", mirror=True)
# the bulb on its short stalk, leaning a little to one side
k.add("stalk", tube([(0, 0.008, 0.86), (0.004, 0.01, 0.91), (0.01, 0.012, 0.945)], [0.036, 0.022, 0.022]), PINK, "head")
def drop(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    V[:, 0] *= 0.55 + 0.6 * t                  # narrow where it leaves the stalk, round at the top
    V[:, 1] *= 0.55 + 0.6 * t
    return V
k.add("bulb", ell((0.02, 0.012, 1.005), (0.056, 0.048, 0.078), rot=(0, 10, 0), deform=drop), PINK, "head")


# The cap's edge: straight across just over the eyes in front, lower round the sides and the back
def brow(P):
    phi = np.abs(np.arctan2(P[:, 0], -(P[:, 1] - HC[1])))
    return 0.772 - 0.04 * smoothstep(0.55, 1.25, phi) - 0.03 * smoothstep(1.7, 2.8, phi)
def head_zone(P):
    return (P[:, 2] > 0.62) * (np.hypot(P[:, 0], P[:, 1] - HC[1]) < 0.124)
k.paint(lambda P: inside(brow(P) - P[:, 2], 0.0045) * head_zone(P), PINK)
k.paint(lambda P: inside(P[:, 2] - brow(P), 0.0045) * head_zone(P) * (np.abs(P[:, 0]) < 0.1), PALE)

# Small pink pouting lips, a slot of a mouth carved in them
k.add("lips", ell((0, -0.072, 0.664), (0.026, 0.022, 0.018)), LIPS, "head")
k.add("mouth", ell((0, -0.094, 0.664), (0.014, 0.01, 0.006)), MOUTH, "head", carve=True)

# A thin neck and a thin pale body, a little broader at the chest
k.add("neck", tube([(0, 0.0, 0.58), (0, -0.004, 0.64), (0, -0.006, 0.68)], [0.022, 0.019, 0.02]), PALE, "head")
k.add("chest", ell((0, 0.0, 0.575), (0.052, 0.04, 0.06)), PALE, "body")
k.add("waist", ell((0, 0.0, 0.5), (0.038, 0.034, 0.06)), PALE, "body")

# Long thin arms out to the sides and a little down, the hands open
k.add("arm", tube([(0.05, 0.0, 0.6), (0.13, -0.012, 0.565), (0.205, -0.03, 0.52), (0.25, -0.05, 0.49)],
                  [0.018, 0.015, 0.014, 0.013]), PALE, "arm{s}", mirror=True)
k.add("hand", ell((0.265, -0.06, 0.482), (0.026, 0.022, 0.016), rot=(0, 25, 0)), PALE, "arm{s}", mirror=True)
for i, (dx, dy) in enumerate(((0.0, -0.014), (0.006, 0.0), (0.0, 0.013))):
    k.add(f"finger{i}", tube([(0.275 + dx, -0.06 + dy, 0.478), (0.3 + dx, -0.068 + dy, 0.464)], [0.008, 0.006]),
          PALE, "arm{s}", mirror=True)
k.add("thumb", tube([(0.255, -0.08, 0.485), (0.27, -0.095, 0.49)], [0.008, 0.006]), PALE, "arm{s}", mirror=True)


# The trousers: a great ball each leg, round below and drawn in toward the waist, joined at the middle
def egg(V):
    V = V.copy()
    t = np.clip((V[:, 2] + 1) / 2, 0, 1)
    s = 1.05 - 0.38 * smoothstep(0.45, 1.0, t)
    V[:, 0] *= s; V[:, 1] *= s
    return V
TB = np.array([0.112, 0.0, 0.27])
TR = (0.15, 0.142, 0.21)
k.add("trouser", ell(TB, TR, deform=egg), PINK, "leg{s}", mirror=True)
k.add("seat", ell((0, 0.01, 0.4), (0.1, 0.09, 0.08)), PINK, "body")

# The yellow rim at the waist: a band round the trousers' top crossing over in a V at the front
def rim_pts(side):
    pts = []
    for a in np.linspace(-math.pi, math.pi, 25):
        dz = 0.07 * math.exp(-(a / 0.45) ** 2)
        r = 0.088 - 0.02 * math.exp(-(a / 0.45) ** 2)
        pts.append((r * math.sin(a), -r * math.cos(a), 0.475 - dz))
    return pts
k.add("rim", tube(rim_pts(1), [0.013] * 25, seg=12, per=4), YELLOW, "body")

# Thin pale ankles drawn to points under the trousers
k.add("ankle", tube([(0.1, 0.0, 0.16), (0.1, -0.004, 0.06), (0.102, -0.012, 0.004)], [0.024, 0.018, 0.006]), PALE,
      "leg{s}", fuse=False, mirror=True)

# A big yellow spot low on each trouser's outer side
sd = unit(0.75, -0.5, -0.3)
k.decal(TB + sd * 0.15, sd, (0.042, 0.042), YELLOW, eps=0.003, rings=12, segs=44, mirror=True)

# Three yellow dots on the cap's front: two above, one between them below
for x, z in ((0.042, 0.83), (0.0, 0.795)):
    p = np.array([x, 0.0, z])
    n = unit(x * 6, -1, (z - 0.79) * 6 + 0.35)
    k.decal(p + n * 0.0 - np.array([0, 0.11, 0]), n, (0.014, 0.014), YELLOW, eps=0.003, rings=8, segs=32,
            mirror=x > 0)

# The eyes: round and white, a large black pupil low in them, a heavy dark lid sloping down toward the nose
CW, CI, CP = srgb("#F6F6FA"), srgb(INK), srgb("#202028")
CL = srgb(PALE)
def top(u):
    return 0.36 + 0.26 * u
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    e = (u / 0.98) ** 2 + (v / 0.98) ** 2
    under = (v < top(u)) & (e < 1)
    c[under] = CI
    c[under & (e < 0.68)] = CW
    pu = np.hypot((u + 0.04) / 0.46, (v + 0.24) / 0.5)
    c[under & (pu < 1)] = CP
    c[under & (np.hypot((u - 0.1) / 0.15, (v + 0.06) / 0.15) < 1)] = (1, 1, 1)
    c[(np.abs(v - top(u)) < 0.16) & (e < 0.98)] = CI
    return c
de = unit(0.4, -0.92, 0.02)
E = np.array([0.04, -0.084, 0.736])
def eyes(state):
    k.decal(E, de, (0.03, 0.03), eye_look(state, eye, CL, CI, line=0.16), eps=0.004, rings=20, segs=56, mirror=True)


k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
