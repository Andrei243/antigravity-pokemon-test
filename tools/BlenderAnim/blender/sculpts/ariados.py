# Ariados sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ariados.py -- <the export's Ariados.glb> <out.glb>
#
# Spinarak grown up (spinarak.py's leg, horn and way of laying marks): from the references, a red spider. A big
# rounded red abdomen, a broad black band across its front that wraps down its sides, black spots on its flanks and
# a small pale spinneret at its tail; a red wedge of a head with a white horn jutting forward and up from the brow,
# two white fangs hooking down under it, and eyes that are black ovals with a white glint in lilac sockets. Six long
# walking legs, a thin yellow thigh to a high knee, then banded purple and yellow to a yellow point; two more legs
# rising from its back, banded the same.
# Skeleton: body (0, 0, 0.15), head (0, -0.15, 0.21), legs (+-0.15, -0.135 / 0.045 / 0.225, 0.225) on leg, mid, hind.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, boxy, srgb, inside, unit, eye_look, args, sibling

sp = sibling("spinarak")
RED, BLACK, PURPLE, YELLOW = "#DE3A4A", "#2A2228", "#8A7CD0", "#F2C04E"
HORN, FANG, LILAC, GLINT = sp.HORN, "#F2F2F0", "#A898DC", "#FFFFFF"
SPIN = "#EAD99A"
E = 0.004

src, out = args()
k = Kit(src, coat="shell")

# --- the frame -------------------------------------------------------------------------------------------------------
AB, ABR = np.array([0.0, 0.1, 0.24]), np.array([0.2, 0.25, 0.17])         # the abdomen
HD, HDR = np.array([0.0, -0.22, 0.215]), np.array([0.142, 0.12, 0.102])    # the head
LEGS = [  # bone, hip, knee, tip (on +X)
    ("leg{s}", (0.13, -0.12, 0.23), (0.25, -0.22, 0.34), (0.27, -0.33, 0.006)),
    ("mid{s}", (0.16, 0.045, 0.23), (0.3, 0.04, 0.36), (0.34, 0.05, 0.006)),
    ("hind{s}", (0.13, 0.2, 0.23), (0.25, 0.27, 0.34), (0.27, 0.38, 0.006)),
]
BANDS = [(0.36, PURPLE), (0.56, YELLOW), (0.8, PURPLE)]

# ---- the abdomen: big and round, a black band across its front, black spots on its flanks
k.add("abdomen", ell(AB, ABR), RED, "body")
k.add("waist", ell((0, -0.1, 0.21), (0.1, 0.08, 0.08)), RED, "body")
def band(P):
    edge = 0.012 * np.sin(P[:, 0] * 30)
    return inside(-(P[:, 1] - (-0.04 + edge)), E) * inside(P[:, 1] - (0.05 + edge), E) * (P[:, 2] > 0.09) \
        * (np.abs(P[:, 0]) < 0.21)
k.paint(band, BLACK, on=["abdomen"])
for s in (1, -1):
    for d, h in (((0.85, 0.55, 0.15), (0.052, 0.045)), ((0.55, 0.85, 0.55), (0.038, 0.034))):
        p, n = sp.on_ell(AB, ABR, (d[0] * s, d[1], d[2]))
        k.decal(p, n, h, BLACK, up=(0, 0, 1), eps=0.004, rings=12, segs=40)
k.add("spinneret", lathe((0, 0.33, 0.25), unit(0, 1, 0.35), [(0, -0.01), (0.03, 0.0), (0.022, 0.03), (0, 0.06)]),
      SPIN, "body", fuse=False)

# ---- the head: a red wedge, flat above, its brow overhanging the face
def wedge(V):
    V = boxy(2.6)(V)
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * (0.8 - 0.15 * np.clip(-V[:, 1], 0, 1)), V[:, 2])
    return V
k.add("head", ell(HD, HDR, rot=(-8, 0, 0), deform=wedge), RED, "head")
sp.horn(k, (0, -0.27, 0.262), (0, -0.78, 0.62), 0.15, 0.032)
k.add("fang", tube([(0.04, -0.3, 0.145), (0.047, -0.335, 0.115), (0.04, -0.35, 0.08), (0.024, -0.34, 0.062)],
                   [0.017, 0.015, 0.011, 0.006]), FANG, "head", fuse=False, mirror=True)

# ---- six walking legs and two rising from the back
for bone, hip, knee, tip in LEGS:
    sp.leg(k, bone[:3], bone, hip, knee, tip, 0.025, YELLOW, BANDS, YELLOW)
sp.leg(k, "back", "body", (0.07, 0.02, 0.36), (0.12, 0.05, 0.44), (0.2, 0.12, 0.66), 0.024, YELLOW, BANDS, YELLOW)

k.build(voxel=0.006, smooth=4)

# ---- the eyes: black ovals with a white glint, in lilac sockets on the face's sides
CL, CB, CW = srgb(LILAC), srgb(BLACK), srgb(GLINT)
def eye(u, v):
    c = np.tile(CL, (len(u), 1))
    c[(u / 0.62) ** 2 + ((v + 0.05) / 0.66) ** 2 < 1] = CB
    c[((u + 0.18) / 0.2) ** 2 + ((v - 0.22) / 0.18) ** 2 < 1] = CW
    return c
def eyes(state):
    p, n = sp.on_ell(HD, HDR, (0.62, -0.75, 0.05))
    k.decal(p, n, (0.05, 0.042), eye_look(state, eye, CL, CB, line=0.16), up=(-0.2, 0, 1), eps=0.004, rings=20,
            segs=56, mirror=True)
k.eye_states(eyes)
k.export(out)
