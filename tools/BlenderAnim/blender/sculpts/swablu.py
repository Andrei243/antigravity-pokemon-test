# Swablu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/swablu.py -- <the export's Swablu.glb> <out.glb>
# A small round sky-blue bird, body and head one ball; two big wings of white cotton cloud, lumpy puffs wider than
# the body, held out at its sides; a big round pale beak on the front with its mouth line below; small round black
# eyes beside it; two long thin feathers sweeping up and back from the crown, two tail feathers down behind, tiny
# pale feet of three toes.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

BLUE, CLOUD, SHADE, BEAK, BEAKLOW, FOOT = "#58B6EA", "#F7F9FC", "#D3DDEC", "#ECEEF2", "#BEC4D0", "#C4CAD6"

# The ball: the body's, with the head's front and top on the head bone so the face follows it
C = np.array([0, 0.0, 0.168])
R = 0.086
k.add("body", ell(C, (R, R * 0.97, R)), BLUE, "body")
k.add("head", ell((0, -0.018, 0.182), (0.077, 0.068, 0.07)), BLUE, "head")

# The cloud wings: puffs of cotton, fused into one lumpy cloud each, a little behind and above the body's middle
W = np.array([0.205, 0.03, 0.19])
PUFFS = [(-0.075, 0.0, -0.005, 0.05), (-0.035, 0.0, 0.045, 0.052), (0.025, 0.006, 0.058, 0.056),
         (0.078, 0.004, 0.036, 0.048), (0.108, 0.0, -0.004, 0.042), (0.066, 0.0, -0.044, 0.046),
         (0.006, 0.0, -0.05, 0.05), (-0.05, 0.0, -0.042, 0.042), (0.012, 0.0, 0.004, 0.066),
         (0.03, -0.042, 0.0, 0.046), (0.02, 0.046, 0.012, 0.048), (-0.03, -0.03, 0.02, 0.04),
         (0.07, -0.03, 0.008, 0.04)]
for i, (dx, dy, dz, r) in enumerate(PUFFS):
    k.add(f"cloud{i}_", ell(W + np.array([dx, dy, dz]), (r, r * 0.85, r * 0.92)), CLOUD, "wing{s}", mirror=True)
# a soft blue-grey shade on the clouds' undersides, as the pictures have it
k.paint(lambda P: smoothstep(W[2] - 0.0, W[2] - 0.07, P[:, 2]) * 0.75, SHADE, on=["cloud"])

# Two tail feathers, pointing down and back
for s in (1, -1):
    pts = [(0.008 * s, 0.06, 0.12), (0.018 * s, 0.11, 0.085), (0.03 * s, 0.15, 0.045)]
    k.add(f"tail{'R' if s > 0 else 'L'}", blade(pts, [0.012, 0.012, 0.003], (s * 1.0, 0, 0.3), 0.4), BLUE, "tail",
          fuse=False)

# The two long crown feathers, from the top of the head up, out and back, curving outward at their tips
for s in (1, -1):
    pts = np.array([(0.008 * s, -0.002, 0.245), (0.026 * s, 0.018, 0.298), (0.062 * s, 0.045, 0.34),
                    (0.112 * s, 0.075, 0.362)])
    n = np.cross(pts[1] - pts[0], pts[3] - pts[0])
    k.add(f"plume{'R' if s > 0 else 'L'}", blade(pts, [0.008, 0.016, 0.014, 0.002], n, 0.35), BLUE, "head",
          fuse=False)

# The big round beak: an upper bill rounded over, a smaller lower bill under it with the mouth's line between
def bill(V):
    V = V.copy()
    t = np.clip(-V[:, 1], 0, 1)
    V[:, 2] -= 0.18 * t ** 2
    return V
k.add("beak", ell((0, -0.084, 0.192), (0.036, 0.032, 0.032), deform=bill), BEAK, "head", fuse=False, surface="shell")
k.add("beaklow", ell((0, -0.078, 0.163), (0.028, 0.024, 0.013)), BEAKLOW, "head", fuse=False, surface="shell")

# Tiny pale feet: a short leg each, three toes down and forward
for s in (1, -1):
    sd = "R" if s > 0 else "L"
    top, ank = np.array([0.03 * s, -0.005, 0.095]), np.array([0.033 * s, -0.012, 0.074])
    k.add(f"leg{sd}", tube([top, ank], [0.009, 0.008]), FOOT, f"leg{sd}", fuse=False, surface="shell")
    for j, a in enumerate((-30, 0, 30)):
        r_ = math.radians(a)
        d = np.array([math.sin(r_) * 0.7, -math.cos(r_) * 0.6, -0.75])
        k.add(f"toe{sd}{j}", tube([ank, ank + d * 0.02], [0.006, 0.004]), FOOT, f"leg{sd}", fuse=False,
              surface="shell")

# The eyes: round and black with a white glint, beside the beak
INK, LID = srgb("#121216"), srgb(BLUE)
def eye(u, v):
    c = np.tile(INK, (len(u), 1))
    c[((u - 0.25) / 0.36) ** 2 + ((v - 0.35) / 0.36) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.64, -0.72, 0.3)
def eyes(state):
    k.decal(C + de * R, de, (0.0125, 0.015), eye_look(state, eye, LID, INK, line=0.2), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.0042)
k.eye_states(eyes)
k.export(out)
