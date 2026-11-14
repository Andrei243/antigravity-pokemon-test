# Duskull sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/duskull.py -- <the export's Duskull.glb> <out.glb>
# A small grey ghost in a hood that rises to a curling wisp and hangs down behind into a pointed hem; a white skull
# mask fills the hood's opening, pointed at the top, two great round eye holes, a row of blunt teeth below; one red
# light glows in one hole; a bone marks the back of the hood; two short stubby arms at the sides.
# The skeleton: body (0, 0, 0.34), head above it (0.44), tail behind and below (0, 0.05, 0.24).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

GREY, GREYDARK, BONE, BONESHADE, HOLE = "#7E7E84", "#6A6A72", "#EEE8D2", "#C8C2AC", "#18141A"
HC = np.array([0.0, 0.01, 0.39])          # the hood's middle
HR = np.array([0.185, 0.19, 0.19])

# --- the hood -----------------------------------------------------------------------------------------------------
k.add("hood", ell(HC, HR), GREY, "body")
# its edges coming forward round the mask's sides, so the mask sits in the hood rather than on it
k.add("cowl", ell((0.137, -0.07, 0.42), (0.045, 0.085, 0.13), rot=(0, -14, 0)), GREY, "body", mirror=True)
# rising to a point at the crown, where the wisp leaves it
k.add("crown", ell((0, 0.04, 0.53), (0.1, 0.1, 0.1)), GREY, "head")
k.add("wisp", tube([(0, 0.045, 0.56), (0.0, 0.05, 0.64), (0.025, 0.035, 0.7), (0.06, 0.01, 0.725), (0.085, -0.005, 0.71)],
                   [0.058, 0.036, 0.024, 0.016, 0.008]), GREY, "head")
# the robe below, gathering behind into a pointed hem that trails back and down
k.add("skirt", ell((0, 0.05, 0.27), (0.15, 0.15, 0.1)), GREY, "body")
k.add("hem", tube([(0, 0.06, 0.25), (0, 0.11, 0.17), (0.0, 0.18, 0.11), (0.0, 0.25, 0.085)],
                  [0.12, 0.085, 0.045, 0.008]), GREY, "tail")
# two short stubby arms hanging at the sides, a little back
k.add("arm", tube([(0.14, 0.07, 0.38), (0.2, 0.08, 0.33), (0.215, 0.085, 0.29)], [0.05, 0.045, 0.04]), GREY, "body",
      mirror=True)

# --- the mask: a shield standing proud in the hood's opening, pointed at the top --------------------------------------
def shield(V):
    V = V.copy()
    t = np.clip(V[:, 2], 0, 1)
    V[:, 0] *= 1 - 0.5 * t                   # narrowing toward the point
    V[:, 2] = np.where(V[:, 2] > 0, V[:, 2] * 1.12, V[:, 2])
    return V
MC = np.array([0.0, -0.12, 0.375])
k.add("mask", ell(MC, (0.168, 0.085, 0.18), deform=shield), BONE, "body", surface="shell")
# The jaw: a lower edge of the mask and four blunt teeth below it, with the hood showing between them
k.add("jaw", ell((0, -0.145, 0.255), (0.108, 0.065, 0.06)), BONE, "body", surface="shell")
for i, x in enumerate((-0.078, -0.026, 0.026, 0.078)):
    k.add(f"tooth{i}", ell((x, -0.16, 0.208), (0.02, 0.034, 0.032), deform=lambda V: V / (np.abs(V) ** 4).sum(1, keepdims=True) ** 0.25),
          BONE, "body", surface="shell")

# --- the bone on the back of the hood: a patch whose outline is a bone, slanting a little -----------------------------
def bone_sdf(u, v):
    a = math.radians(14)
    u, v = u * math.cos(a) - v * math.sin(a), u * math.sin(a) + v * math.cos(a)
    bar = max(abs(u) - 0.6, abs(v) - 0.2)
    lobes = min(math.hypot(u - sx * 0.66, v - sv * 0.22) - 0.26 for sx in (1, -1) for sv in (1, -1))
    return min(bar, lobes)
def bone_outline(th):
    c, s = math.cos(th), math.sin(th)
    r = 0.0
    while r < 1.2 and bone_sdf(r * c, r * s) < 0:
        r += 0.004
    return r
bn = unit(0, 1, 0.5)
k.decal(HC + bn * 0.17, bn, (0.1, 0.1), BONE, eps=0.004, outline=bone_outline, rings=10, segs=96)

k.build(voxel=0.006, smooth=3, band=0.006)          # a narrow band: the mask's edge against the hood stays crisp

# --- the eyes: two great round holes in the mask, a rim of the bone's shade, dark within; one red light in the right ----
INK, LID = srgb("#2A2228"), srgb(BONE)
DARK, RIM = srgb(HOLE), srgb(BONESHADE)
CORE, RED, DEEP = srgb("#FFD2DA"), srgb("#F0405A"), srgb("#7A1424")
def hole(lit):
    def look(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(DARK, (len(u), 1))
        if lit:
            g = smoothstep(0.62, 0.2, r)[:, None]
            c = c * (1 - g) + DEEP[None] * g
            c[r < 0.48] = RED
            c[r < 0.22] = CORE
        c[r > 0.775] = RIM                  # between two rings of the patch, or the edge ticks
        return c
    return look
EH = 0.066
EX, EZ = 0.079, 0.395
def eyes(state):
    for side in (1, -1):
        de = unit(0.32 * side, -1, 0.06)
        c = np.array([EX * side, -0.2, EZ])
        look = eye_look(state, hole(side == 1), LID, INK, line=0.12)
        col = look if side == 1 else (lambda u, v, f=look: f(-u, v))
        k.decal(c, de, (EH, EH), col, eps=0.005, rings=20, segs=56)
        if side == 1 and state in ("Open", "Fierce"):
            # the light itself, a lens lying on the hole's red: the one thing about it that glows
            hit = TREE.ray_cast(Vector(c + de * 0.35), Vector(-de))[0]
            right = np.cross((0, 0, 1.0), de); right /= np.linalg.norm(right)
            up = np.cross(de, right)
            V, F = ell((0, 0, 0), (0.019, 0.019, 0.005))
            V = V @ np.stack([right, up, de], axis=1).T + np.array(hit) + de * 0.004
            k.add("light", (V, F), "#FF7A8E", "body", fuse=False, glow="#FF3A55", surface="skin")

from mathutils import Vector
from mathutils.bvhtree import BVHTree
_P, _F, _W = k.surfaces[k.name]
TREE = BVHTree.FromPolygons(_P.tolist(), _F)

k.eye_states(eyes)
k.export(out)
