# Shieldon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/shieldon.py -- <the export's Shieldon.glb> <out.glb>
#
# The design, from the references: a small tan quadruped wearing a great dark grey oval shield on its head, leaning
# back over its shoulders, a thick light grey band across the shield's cut foot; under the band the face: big round eyes, white
# with large black pupils, in grey cheeks with tan between them, and a grey snout standing forward with a second pale
# band arched over it. A grey hump on the back behind the shield, a broad pointed tan tail, short legs each with one
# white claw. (The eyes are not on the shield: they look out from under its band.)
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

TAN, SHIELD, PANEL, GREY, SNOUT, BAND, CLAW = \
    "#EAC97A", "#5E6066", "#666970", "#62656B", "#696C72", "#CDD0D4", "#D6D8DC"
INK, WHITE = "#141416", "#FFFFFF"

H = np.array([0.0, -0.10, 0.225])          # the head's middle (the head bone is at 0, -0.08, 0.26)
HR = np.array([0.14, 0.10, 0.085])

# --- the body ----------------------------------------------------------------------------------------------------
k.add("body", ell((0, 0.07, 0.19), (0.135, 0.16, 0.125)), TAN, "body")
k.add("neck", ell((0, -0.03, 0.21), (0.115, 0.08, 0.1)), TAN, "body")
k.add("head", ell(H, HR), TAN, "head")
k.add("snout", ell((0, -0.195, 0.14), (0.085, 0.08, 0.062), deform=boxy(2.8)), SNOUT, "head")
k.add("tail", tube([(0, 0.19, 0.2), (0, 0.27, 0.215), (0, 0.33, 0.235)], [0.075, 0.045, 0.008]), TAN, "tail")

# The legs: short and thick; one white claw at the front of each foot
for name, y, bone in (("leg", -0.06, "leg{s}"), ("hind", 0.2, "hind{s}")):
    k.add(name, tube([(0.105, y, 0.15), (0.115, y - 0.005, 0.045)], [0.05, 0.048]), TAN, bone, mirror=True)
    k.add(name + "foot", ell((0.115, y - 0.02, 0.03), (0.05, 0.058, 0.03)), TAN, bone, mirror=True)
    k.add(name + "claw", tube([(0.115, y - 0.058, 0.03), (0.117, y - 0.072, 0.014), (0.118, y - 0.08, 0.003)],
                              [0.014, 0.009, 0.002]), CLAW, bone, fuse=False, mirror=True, surface="shell")

# The hump on the back, grey, showing behind the shield
k.add("hump", ell((0, 0.13, 0.295), (0.06, 0.075, 0.035), deform=boxy(2.4)), SHIELD, "body", fuse=False,
      surface="shell")

# The grey sides of the face round the eyes, tan between them
k.paint(lambda P: inside(0.052 - np.abs(P[:, 0]), 0.004) * (P[:, 1] < -0.03) * (P[:, 2] > 0.14), GREY, on=["head"])

# --- the shield: a dark oval leaning back, its front domed, its back flatter -----------------------------------------
LEAN = 28
A = -LEAN                                     # its lean: its up runs back as it rises
UPV = np.array([0, math.sin(math.radians(LEAN)), math.cos(math.radians(LEAN))])
FRONT = np.array([0, -math.cos(math.radians(LEAN)), math.sin(math.radians(LEAN))])
SW, SH, ST = 0.185, 0.23, 0.12                 # half width, half height (an upright egg), half thickness (deep)
FOOT = 0.55                                   # below this share of its half height the egg is cut nearly flat
FLAT = FOOT + (1 - FOOT) * 0.3                # where its foot ends, as a share of its half height
SC = np.array([0.0, -0.13, 0.27]) + UPV * SH * FLAT     # its middle: its foot sits on the brow


def dome(V):
    """Half an egg in front, a shallow back that the head fills, the foot cut wide and nearly flat for the band."""
    V = V.copy()
    back = V[:, 1] > 0
    V[back, 1] *= 0.4
    V[~back, 1] = -np.abs(V[~back, 1]) ** 0.85
    low = V[:, 2] < -FOOT
    V[low, 2] = -FOOT + (V[low, 2] + FOOT) * 0.3
    V[:, 0] *= 1 - 0.12 * np.clip(V[:, 2], 0, 1)          # an egg: a little narrower toward its top
    return V


k.add("shield", ell(SC, (SW, ST, SH), rot=(A, 0, 0), deform=dome), SHIELD, "head", fuse=False, surface="shell")
# the inner oval of its face, only a shade lighter than its border
k.add("panel", ell(SC + FRONT * 0.084 + UPV * 0.04, (0.13, 0.04, 0.17), rot=(A, 0, 0), deform=dome), PANEL, "head",
      fuse=False, surface="shell")
# the back of the head filling the shield from behind, so no neck shows under it
k.add("crown", ell((0, -0.03, 0.31), (0.15, 0.115, 0.11)), TAN, "head")

# The thick band along the shield's cut foot, across its front and wrapping round its sides toward the back
pts = []
LZ = -SH * (FOOT + 0.03)
W0 = SW * math.sqrt(1 - FOOT ** 2) * 1.02
D0 = ST * (1 - FOOT ** 2) ** 0.425
for psi in np.linspace(-1.95, 1.95, 17):
    off = D0 * math.cos(psi) if math.cos(psi) > 0 else 0.4 * D0 * math.cos(psi)
    pts.append(SC + np.array([W0 * math.sin(psi), 0, 0]) + UPV * (LZ + 0.012 * psi * psi) + FRONT * (off - 0.01))
k.add("band", tube(pts, [0.03] * len(pts), seg=18, per=5), BAND, "head", fuse=False, surface="shell")

# The pale band arched over the snout
arc = []
for a in np.linspace(-1.45, 1.45, 13):
    arc.append((0.09 * math.sin(a), -0.18 - 0.09 * math.cos(a), 0.163 + 0.026 * math.cos(a)))
k.add("noseband", tube(arc, [0.021] * 13, seg=16, per=4), BAND, "head", fuse=False, surface="shell")

# --- the eyes: round, white, a large black pupil a little inward, a dark rim ------------------------------------------
CW, CI, LID = srgb(WHITE), srgb(INK), srgb(GREY)


def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    c[u * u + v * v > 0.825 ** 2] = CI            # the rim's edge between two rings of the patch
    return c


EH = (0.036, 0.038)


def eyes(state):
    for s in (1, -1):
        e = np.array([s * 0.083, -0.177, 0.218])
        n = unit(s * 0.55, -1, 0.05)
        look = eye_look(state, eye, LID, CI, line=0.16)
        lk = look if s > 0 else (lambda f: (lambda u, v: f(-u, v)))(look)
        k.decal(e, n, EH, lk, eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            # the pupil and its glint, patches of their own so that their edges are round
            right = np.cross((0, 0, 1.0), n); right /= np.linalg.norm(right)
            up = np.cross(n, right)
            low = 0.0 if state == "Open" else -0.008
            pc = e - s * right * 0.005 + up * (low - 0.001)
            k.decal(pc, n, (0.0185, 0.021 if state == "Open" else 0.012), INK, eps=0.0065, rings=10, segs=36)
            k.decal(pc + s * right * 0.0045 + up * (0.0075 + low * 0.5), n, (0.0055, 0.0055), WHITE, eps=0.009,
                    rings=4, segs=16)


k.build(voxel=0.005, smooth=3)
k.eye_states(eyes)
k.export(out)
