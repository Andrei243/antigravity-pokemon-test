# Chatot sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chatot.py -- <the export's Chatot.glb> <out.glb>
# A small parrot: a black head with a crest shaped like an eighth note's stem and flag, a big pink hooked beak, pink
# half-lidded eyes with a dark red pupil, a white ruffled collar, a yellow chest over a green belly, blue wings and
# back, blue tail feathers and a black tail like a metronome's pointer (a stick, a ball and a spike), yellow feet.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLACK, BLUE, LIGHT, YELLOW, GREEN = "#3A3C44", "#2FA0E6", "#7CCDF4", "#F7D23E", "#3CAE5A"
WHITE, NECK, PINK, BEAKLOW, FOOT, CLAW = "#F7F8FA", "#D6DFE9", "#EE7A92", "#CC5C74", "#F4C42E", "#B08A24"

HC = np.array([0, -0.06, 0.34])          # the head's centre
HR = np.array([0.08, 0.08, 0.077])

# The body: an egg leaning forward, its back sloping down to the tail; a short neck under the collar
k.add("body", ell((0, 0.025, 0.14), (0.096, 0.12, 0.108), rot=(-10, 0, 0)), BLUE, "body")
k.add("neck", tube([(0, 0.0, 0.22), (0, -0.035, 0.3)], [0.055, 0.05]), NECK, "head")
k.add("head", ell(HC, HR), BLACK, "head")

# The note: a stem straight up from the back of the head, and its flag curving back and down to a point
k.add("stem", tube([(0, -0.02, 0.38), (0, -0.01, 0.45), (0, 0.0, 0.5)], [0.02, 0.014, 0.014]), BLACK, "head")
flag = [(0, 0.0, 0.5), (0, 0.035, 0.493), (0, 0.068, 0.462), (0, 0.086, 0.418), (0, 0.088, 0.375), (0, 0.075, 0.338)]   # loose, so its edges stay crisp
k.add("flag", blade(flag, [0.016, 0.036, 0.044, 0.04, 0.026, 0.003], (1, 0, 0), 0.36), BLACK, "head", fuse=False)

# The wings: blue, close at the sides, a lighter zigzag across each, and long feathers sweeping back past the rump
k.add("wing", ell((0.092, 0.045, 0.17), (0.03, 0.105, 0.08), rot=(-22, 0, 4)), BLUE, "wing{s}", mirror=True)
k.paint(lambda P: inside(np.abs(P[:, 2] - (0.18 + 0.01 * tri((P[:, 1] + 0.1) / 0.045)
                                         - 0.25 * (P[:, 1] - 0.04))) - 0.007, 0.003), LIGHT, on=["wing"])
k.add("wingtip", blade([(0.084, 0.11, 0.15), (0.074, 0.17, 0.12), (0.052, 0.225, 0.09)], [0.03, 0.024, 0.004],
                       (1, 0, 0.25), 0.38), BLUE, "wing{s}", fuse=False, mirror=True)

# The chest yellow under the collar, the belly green below it, both on the front of the body
def yellow(P):
    return inside(P[:, 1] + 0.035 - 0.25 * (P[:, 2] - 0.2), 0.003) * inside(0.155 - P[:, 2], 0.003)
def green(P):
    return inside(P[:, 1] + 0.005 - 0.4 * (P[:, 2] - 0.12), 0.003) * inside(P[:, 2] - 0.155, 0.003)
k.paint(yellow, YELLOW, on=["body"])
k.paint(green, GREEN, on=["body"])

# Blue tail feathers out behind the rump, flat, under the metronome
k.add("tailfeathers", blade([(0, 0.1, 0.14), (0, 0.17, 0.11), (0, 0.235, 0.088)], [0.032, 0.04, 0.006], (0, 0, 1), 0.3),
      BLUE, "tail", fuse=False)
# The metronome: a black stick up and back, a ball, a spike beyond it
k.add("stick", tube([(0, 0.09, 0.19), (0, 0.17, 0.22), (0, 0.235, 0.248)], [0.011, 0.009, 0.008]), BLACK, "tail",
      fuse=False)
k.add("ball", ell((0, 0.242, 0.251), (0.024, 0.024, 0.024)), BLACK, "tail", fuse=False)
k.add("spike", tube([(0, 0.255, 0.257), (0, 0.283, 0.271), (0, 0.31, 0.285)], [0.011, 0.006, 0.0008]), BLACK, "tail",
      fuse=False)

# The white collar: two rings of pointed ruffles drooping round the neck, their seams at the back
def collar(z0, z1, r0, r1, tips, n, phase):
    def at(u, v):
        uu = 0.015 + 0.985 * u
        a = math.pi * (1 + 2.04 * uu) + phase
        R = r1 + tips * (0.5 + 0.5 * tri(a * n / (2 * math.pi)))
        r = r0 + v * (R - r0)
        z = z0 - v * (z0 - z1) - 0.012 * v * v
        return np.stack([np.sin(a) * r, -0.025 - np.cos(a) * r, z], axis=-1)
    return sheet(at, 120, 8, 0.009)
k.add("collar", collar(0.282, 0.212, 0.05, 0.104, 0.022, 11, 0.0), WHITE, "head", fuse=False)
k.add("collartop", collar(0.305, 0.252, 0.045, 0.088, 0.018, 9, 0.17), WHITE, "head", fuse=False)

# The beak: a big pink upper bill hooked down at its tip, a darker lower bill under it
def hook(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.42 * t
    V[:, 2] -= 0.6 * t ** 2
    V[:, 2] *= 1 - 0.15 * t
    return V
k.add("beak", ell((0, -0.13, 0.338), (0.034, 0.05, 0.036), deform=hook), PINK, "head", fuse=False, surface="shell")
k.add("beaklow", ell((0, -0.118, 0.304), (0.022, 0.03, 0.012), rot=(-10, 0, 0)), BEAKLOW, "head", fuse=False,
      surface="shell")

# Short yellow legs, three toes forward and one back, each with a dark claw
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    top, ank = np.array([0.04 * s, 0.0, 0.08]), np.array([0.043 * s, -0.016, 0.02])
    k.add(f"leg{sd}", tube([top, ank], [0.013, 0.011]), FOOT, f"leg{sd}", fuse=False, surface="shell")
    for j, (a, ln) in enumerate(((-28, 0.034), (0, 0.04), (28, 0.034), (180, 0.024))):
        r_ = math.radians(a)
        d = np.array([math.sin(r_) * s, -math.cos(r_), -0.35]); d /= np.linalg.norm(d)
        tip = ank + d * ln
        k.add(f"toe{sd}{j}", tube([ank, tip], [0.008, 0.006]), FOOT, f"leg{sd}", fuse=False, surface="shell")
        k.add(f"claw{sd}{j}", tube([tip, tip + d * 0.008 + np.array([0, 0, -0.006])], [0.005, 0.001]), CLAW, f"leg{sd}",
              fuse=False, surface="shell")

# The eyes: a pink ring of skin round a white eye cut flat on top by a heavy lid, a dark red pupil
INK, LID, RED = srgb("#24161C"), srgb(PINK), srgb("#6A1A2A")
def eye(u, v):
    c = np.tile(LID, (len(u), 1))
    white = (u / 0.78) ** 2 + (v / 0.78) ** 2 < 1
    c[white & (v < 0.18)] = (1, 1, 1)
    c[white & (((u + 0.05) / 0.3) ** 2 + ((v + 0.12) / 0.42) ** 2 < 1) & (v < 0.18)] = RED
    c[white & (np.abs(v - 0.2) < 0.16)] = INK
    return c
de = unit(0.66, -0.62, 0.3)
def eyes(state):
    k.decal(HC + de * 0.08, de, (0.03, 0.027), eye_look(state, eye, LID, INK, line=0.17), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0045)
k.eye_states(eyes)
k.export(out)
