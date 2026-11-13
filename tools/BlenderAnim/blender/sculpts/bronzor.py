# Bronzor sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/bronzor.py -- <the export's Bronzor.glb> <out.glb>
#
# The design, from the references: an old bronze mirror floating upright, a thin disc facing forward. A raised rim of a
# lighter blue-green round its edge, set with six dark round knobs (one at the top, one at the bottom, two on each
# side); inside the rim a darker face with a low domed boss, a ring line round it and four small round studs on that
# line at the diagonals; a round knob of a nose in the middle; two yellow eyes in a dark outline, each with a dark
# upright slit of a pupil, either side of the nose. The back is flat, with a raised pattern of leaf-like marks round a
# middle disc.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="metal")

RIM, FACE, BOSS, KNOB = "#4AA9C6", "#2E86A3", "#2B7D9A", "#1F6681"
LINE, STUD, BACKMARK = "#174C60", "#55B4D2", "#2A7F9B"
EYE, EYERIM, PUPIL = "#F4C332", "#1B2A33", "#141A20"

C = np.array([0.0, 0.0, 0.378])            # the disc's middle, on the body bone
RD = 0.25                                  # the disc's radius


def disc(V):
    """A unit sphere made a slab: flat faces, a rounded edge."""
    V = V.copy()
    V[:, 1] = np.sign(V[:, 1]) * np.abs(V[:, 1]) ** 0.55
    return V


# The disc and its rim
k.add("disc", ell(C, (RD, 0.048, RD), deform=disc), RIM, "body")
th = np.linspace(0, 2 * math.pi, 73)
th = np.concatenate([th, th[1:4]])          # overlap the ends: the loop closes inside itself
rr = 0.24 + 0 * th
ring = np.stack([rr * np.cos(th), np.full_like(th, -0.012), rr * np.sin(th)], axis=1) + C
k.add("rim", tube(ring, np.full(len(ring), 0.036), seg=14, per=3), RIM, "body")

# The boss in the middle, low and domed, and the nose's knob
k.add("boss", ell(C + (0, -0.026, 0), (0.17, 0.042, 0.17)), BOSS, "body")
k.add("nose", ell(C + (0, -0.085, -0.008), (0.04, 0.036, 0.046)), BOSS, "body")

# The face inside the rim, darker; the boss its own shade; the ring line and its four studs
def rad(P):
    return np.sqrt((P[:, 0] - C[0]) ** 2 + (P[:, 2] - C[2]) ** 2)
k.paint(lambda P: inside(rad(P) - 0.2, 0.003) * (P[:, 1] < -0.03), FACE)
k.paint(lambda P: inside(rad(P) - 0.168, 0.003) * (P[:, 1] < -0.04), BOSS)
k.paint(lambda P: inside(np.abs(rad(P) - 0.132) - 0.0035, 0.002) * (P[:, 1] < -0.04), LINE)
for a in (45, 135, 225, 315):
    sc = C + 0.132 * np.array([math.cos(math.radians(a)), 0, math.sin(math.radians(a))])
    d = lambda P, sc=sc: np.sqrt((P[:, 0] - sc[0]) ** 2 + (P[:, 2] - sc[2]) ** 2)
    k.paint(lambda P, d=d: inside(d(P) - 0.02, 0.002) * (P[:, 1] < -0.04), LINE)
    k.paint(lambda P, d=d: inside(d(P) - 0.0155, 0.002) * (P[:, 1] < -0.04), STUD)
# a thin dark groove where the face meets the rim
k.paint(lambda P: inside(np.abs(rad(P) - 0.203) - 0.003, 0.002) * (P[:, 1] < -0.03), LINE)

# The back: a raised middle disc and six leaf-like marks round it, pointing out between the knobs
k.add("backmid", ell(C + (0, 0.046, 0), (0.052, 0.02, 0.052)), BACKMARK, "body")
for i in range(6):
    a = math.radians(60 * i)
    d = np.array([math.cos(a), 0, math.sin(a)])
    m = C + d * 0.125 + (0, 0.044, 0)
    k.add(f"backleaf{i}", ell(m, (0.066, 0.02, 0.026), rot=(0, -math.degrees(a), 0),
                              deform=lambda V: V * np.array([1, 1, 1])[None] * (1 - 0.55 * np.clip(np.abs(V[:, :1]), 0, 1) ** 2)),
          BACKMARK, "body")
k.paint(lambda P: inside(np.abs(rad(P) - 0.2) - 0.0035, 0.002) * (P[:, 1] > 0.035), LINE)

# The six knobs on the rim: loose, so they stay round and crisp
for i in range(6):
    a = math.radians(90 + 60 * i)
    p = C + 0.272 * np.array([math.cos(a), 0, math.sin(a)]) + (0, -0.004, 0)
    k.add(f"knob{i}", ell(p, (0.05, 0.05, 0.05)), KNOB, "body", fuse=False, surface="metal")

# The eyes: yellow, an upright oval in a dark outline, an upright slit of a pupil a little inward
CE, CR, CP = srgb(EYE), srgb(EYERIM), srgb(PUPIL)
def eye(u, v):
    c = np.tile(CE, (len(u), 1))
    c[u * u + v * v > 0.74] = CR
    c[((u + 0.08) / 0.17) ** 2 + (v / 0.5) ** 2 < 1] = CP
    c[((u - 0.32) / 0.13) ** 2 + ((v - 0.42) / 0.13) ** 2 < 1] = srgb("#FFF3C4")
    return c
LIGHT = srgb("#8CD6EA")
def eyes(state):
    for s in (1, -1):
        e = C + np.array([s * 0.104, -0.06, 0.004])
        n = unit(s * 0.18, -1, 0)
        look = eye_look(state, eye, srgb(BOSS), LIGHT, line=0.16)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, n, (0.039, 0.047), lk, eps=0.003, rings=20, segs=56)

k.build(voxel=0.005, smooth=3)
k.eye_states(eyes)
k.export(out)
