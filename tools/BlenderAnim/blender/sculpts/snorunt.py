# Snorunt sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/snorunt.py -- <the export's Snorunt.glb> <out.glb>
#
# The design, from the references: a pale straw cloak shaped like a cone with a rounded tip, covering the body down
# to its feet; at its front an opening coming to a point at the top and rounded below, edged with a thick orange rim
# that runs on down the cloak's front to the hem; in the opening the dark blue-grey face, set back, with two small
# upright pale blue eyes and a wide white grin of flat teeth; two round dark hands clasped one over the other below
# the grin; two stubby dark feet under the hem.
# The skeleton: body (0, 0, 0.3), head (0, -0.06, 0.3), arms (+-0.06, -0.12, 0.16), legs (+-0.06, -0.04, 0.06).
# Everything but the hands and feet is on the body's bone, as Duskull's hood: the face lies in the cloak's opening and
# a head turning alone would push it through the rim.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, sheet, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

STRAW, RIM, DARK, WHITE, LINE, ICE = "#F2CF7E", "#F07434", "#474D58", "#F6F6F2", "#A8ACB4", "#4CB8EC"
HEM, TOP = 0.075, 0.59            # the cloak's hem and the top of its sheet (a ball rounds the tip above)
SQ = 0.92                          # the cone a little shallower front to back than across


def radius(z):
    """The cloak's radius across at a height: full at the hem, narrowing ever faster to the tip."""
    t = np.clip((np.asarray(z, float) - HEM) / (0.6 - HEM), 0, 1)
    return 0.215 * (1 - t ** 1.5) ** 0.9


def opening(z):
    """Half the opening's width seen from the front: a point at the top, widest just under the eyes, a rounded
    bottom behind the hands, then the narrow split of the cloak's front down to the hem."""
    z = np.asarray(z, float)
    top, wide, W = 0.524, 0.27, 0.125
    upper = W * np.clip((top - z) / (top - wide), 0, 1) ** 0.92
    lower = W * np.sqrt(np.clip(1 - ((wide - z) / 0.128) ** 2, 0, 1))
    return np.maximum(np.where(z >= wide, upper, lower), np.where(z < top, 0.008, 0.0))


def edge(z):
    """The angle round from the front at which the cloak's sheet begins, at a height."""
    return np.maximum(np.arcsin(np.clip(opening(z) / radius(z), 0, 0.98)), 0.004)


def at_angle(th, z, r_scale=1.0):
    r = radius(z) * r_scale
    return np.stack([r * np.sin(th), -SQ * r * np.cos(th), z], axis=-1)


# --- the cloak: a loose sheet round from one edge of the opening to the other, so its edges stay crisp ---------------
def cloak(u, v):
    z = HEM + v * (TOP - HEM)
    th0 = edge(z)
    th = th0 + u * (2 * math.pi - 2 * th0)
    return at_angle(th, z)
k.add("cloak", sheet(cloak, 150, 90, 0.01), STRAW, "body", fuse=False)
k.add("tip", ell((0, 0, 0.588), (0.0145, 0.0135, 0.014)), STRAW, "body", fuse=False)

# The orange rim along the opening's edge, up one side, over the point and down the other, and on down the split
zs = np.concatenate([np.linspace(HEM + 0.006, 0.5, 260), np.linspace(0.5, 0.524, 60)])
side = at_angle(edge(zs), zs)
line = np.vstack([side, side[::-1][1:] * np.array([-1, 1, 1])])
seg = np.linalg.norm(np.diff(line, axis=0), axis=1)
s = np.concatenate([[0], np.cumsum(seg)])
S = np.linspace(0, s[-1], 90)
pts = np.stack([np.interp(S, s, line[:, i]) for i in range(3)], axis=1)
rad = np.where(pts[:, 2] < 0.15, 0.0085, 0.0085 + 0.0075 * smoothstep(0.15, 0.2, pts[:, 2]))
rad = np.where(pts[:, 2] > 0.5, rad * (1 - 0.25 * smoothstep(0.5, 0.524, pts[:, 2])), rad)
k.add("rim", tube(pts, rad, seg=14, per=4), RIM, "body", fuse=False)

# --- the dark body inside the cloak, its face showing set back in the opening -----------------------------------------
zi = np.linspace(0.07, 0.5, 16)
V, F = tube([(0, 0, z) for z in zi], 0.88 * radius(zi), seg=40, per=6)
V[:, 2] = np.maximum(V[:, 2], 0.07)
V[:, 1] *= SQ
k.add("inner", (V, F), DARK, "body")

# Two stubby feet under the hem
k.add("foot", ell((0.076, -0.045, 0.045), (0.044, 0.05, 0.046)), DARK, "leg{s}", mirror=True)

# Two round hands clasped below the grin, one over the other and a little to the left, from arms hidden inside
for side_, (hx, hz) in (("R", (0.006, 0.172)), ("L", (-0.03, 0.115))):
    bone = "arm" + side_
    jx = 0.06 if side_ == "R" else -0.06
    hy = -SQ * radius(hz) - 0.022
    k.add("hand" + side_, ell((hx, hy, hz), (0.036, 0.034, 0.036)), DARK, bone, fuse=False)
    k.add("arm" + side_, tube([(jx, -0.12, 0.16), (hx * 0.5 + jx * 0.5, (hy - 0.12) / 2, (hz + 0.16) / 2), (hx, hy + 0.01, hz)],
                              [0.022, 0.022, 0.02]), DARK, bone, fuse=False)

# --- the grin: one white patch, flat along its top and round below, with three lines between the teeth ---------------
DK = srgb(DARK)
def grin_outline(th):
    s_ = math.sin(th)
    return min(1.0, 0.42 / s_) if s_ > 0.05 else 1.0
k.decal((0, -0.16, 0.232), unit(0, -1, 0.32), (0.098, 0.058), WHITE, eps=0.004, outline=grin_outline, rings=16, segs=72)
# the lines between the teeth, each a thin patch of its own over the white (a line in the grin's own polar grid frays)
for x in (-0.049, 0.0, 0.049):
    k.decal((x, -0.16, 0.224), unit(x * 3, -1, 0.32), (0.0045, 0.036), LINE, eps=0.0058, rings=4, segs=20)

k.build(voxel=0.0065, smooth=3, band=0.008)

# --- the eyes: small upright ovals of pale blue, nothing else; on the dark face the closed states draw a pale line -----
BLUE, PALE = srgb(ICE), srgb("#BCE6F8")
def eye(u, v):
    c = np.tile(BLUE, (len(u), 1))
    return c
de = unit(0.42, -0.9, 0.36)
def eyes(state):
    k.decal((0.072, -0.14, 0.302), de, (0.0155, 0.025), eye_look(state, eye, DK, PALE, line=0.23), eps=0.004,
            rings=20, segs=56, mirror=True)

k.eye_states(eyes)
k.export(out)
