# Mismagius sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/mismagius.py -- <the export's Mismagius.glb> <out.glb>
# Grown from misdreavus.py: the same yellow eyes with red irises, dark pupils and glints under a straight lid, red
# gems with a light glint, and locks that fray into pink tips.
#
# The design, from the references: a floating witch. A wide pointed witch's hat of its own dark purple body: a cone
# whose tip bends over to one side, a broad brim tilted back so that the small face looks out from under its front,
# drooping at the sides, its edge wavy; pink-tipped tufts like ragged hair round the crown's base and the crown, and
# sticking out from under the brim at the sides. A small round face: angry yellow eyes with red irises, a zigzag grin.
# A curl of the cloak round the neck to one side. The body a cloak hanging from the neck, three red-pink oval gems
# across its chest (the middle one a little lower), flaring below into a ragged hem of points, its side panels edged in
# pink and two long wavy tails hanging lowest, their tips pink. The skeleton is a body and a head.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils.kdtree import KDTree
from sculptkit import Kit, ell, tube, blade, sheet, srgb, inside, smoothstep, unit, tri, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

PURPLE, UNDER, PINK, DEEPPINK = "#62488E", "#3E2C62", "#EC8ACB", "#D262AE"
GEM, GEMDEEP, GEMLIGHT = "#E0465E", "#A82440", "#FFC0C6"
P_, U_, K_, D_ = srgb(PURPLE), srgb(UNDER), srgb(PINK), srgb(DEEPPINK)

# Loose pieces coloured vertex by vertex: added in one colour, recoloured once the body is built (their points are
# copied into the mesh as they are, so they are found again exactly)
graded = []
def gadd(name, shape, colours, bone, surface=None):
    V, F = shape
    k.add(name, (V, F), PURPLE, bone, fuse=False, surface=surface)
    graded.append((np.array(V, float), np.array(colours, float)))

def recolour(ob):
    V = np.vstack([g[0] for g in graded]); C = np.vstack([g[1] for g in graded])
    kd = KDTree(len(V))
    for i, v in enumerate(V.tolist()):
        kd.insert(v, i)
    kd.balance()
    me = ob.data
    n = len(me.vertices)
    co = np.empty(n * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(n, 3)
    vc = {}
    for i, p in enumerate(co.tolist()):
        _, j, d = kd.find(p)
        if d < 1e-6:
            vc[i] = C[j]
    loops = np.empty(len(me.loops), np.int32); me.loops.foreach_get("vertex_index", loops)
    attr = me.color_attributes["Color"]
    rgba = np.empty(len(me.loops) * 4, np.float32); attr.data.foreach_get("color", rgba); rgba = rgba.reshape(-1, 4)
    for li, vi in enumerate(loops.tolist()):
        if vi in vc:
            rgba[li, :3] = vc[vi]
    attr.data.foreach_set("color", rgba.ravel())


def spike(name, root, d, length, width, flat=(0, 1, 0), pink_from=0.45, bone="head"):
    """A ragged tuft: a flat point from root along d, purple at the root and pink toward its tip."""
    d = unit(*d); root = np.array(root, float)
    pts = [root, root + d * length * 0.45, root + d * length]
    V, F = blade(pts, [width, width * 0.6, 0.003], flat, 0.32)
    t = np.clip(((V - root) @ d) / length, 0, 1)[:, None]
    m = smoothstep(pink_from, pink_from + 0.25, t)
    gadd(name, (V, F), P_ * (1 - m) + K_ * m, bone)

def tuft(name, root, d, n, length, width, spread=0.5, flat=(0, 1, 0), bone="head"):
    """A tuft of n spikes fanning about d, the middle one longest."""
    d = unit(*d)
    side = np.cross(d, unit(*flat)); side /= np.linalg.norm(side)
    for i in range(n):
        a = (i - (n - 1) / 2) / max((n - 1) / 2, 1)
        dd = d + side * a * spread
        spike(f"{name}{i}", np.array(root) + side * a * width * 1.2, dd, length * (1 - 0.3 * abs(a)), width, flat,
              bone=bone)


# ---- the head and face: small and round, looking out from under the brim's front
H = np.array([0.0, -0.005, 0.655])
k.add("head", ell(H, (0.1, 0.095, 0.1)), PURPLE, "head")
k.add("face", ell((0, -0.04, 0.645), (0.085, 0.065, 0.08)), PURPLE, "head")
k.add("crownroot", ell((0, 0.01, 0.74), (0.085, 0.08, 0.05)), PURPLE, "head")
# the neck and the cloak's root
k.add("neck", ell((0, 0.0, 0.565), (0.055, 0.05, 0.05)), PURPLE, "body")
k.add("chest", ell((0, 0.005, 0.48), (0.07, 0.06, 0.09)), PURPLE, "body")
# the curl round the neck to +X, a fold of the cloak sweeping round and out
k.add("curl", tube([(-0.05, -0.075, 0.585), (0.0, -0.095, 0.572), (0.06, -0.075, 0.575), (0.098, -0.03, 0.592),
                    (0.1, 0.02, 0.615)], [0.022, 0.028, 0.028, 0.024, 0.014]), PURPLE, "body")

# The zigzag grin: an ink zigzag with pink in its teeth
INK, CP, MP = srgb("#1C1226"), srgb(PURPLE), srgb("#E486B4")
def grin(u, v):
    c = np.tile(CP, (len(u), 1))
    line = 0.35 * tri(u * 1.25 + 0.25) - 0.05 + 0.15 * u * u
    inner = (v < line + 0.22) & (v > line - 0.32) & (np.abs(u) < 0.85)
    c[inner & (v < line - 0.02)] = MP
    c[(np.abs(v - line) < 0.18) & (np.abs(u) < 0.92)] = INK
    return c
k.decal((0.0, -0.105, 0.608), unit(0, -1, -0.35), (0.04, 0.02), grin, eps=0.004, rings=12, segs=56)

# ---- the hat: a cone bending over to +X and a little back, and a brim tilted back
TIP = [(0.0, 0.015, 0.735), (0.004, 0.02, 0.84), (0.02, 0.035, 0.92), (0.06, 0.055, 0.975), (0.115, 0.07, 0.995)]
k.add("cone", tube(TIP, [0.115, 0.08, 0.045, 0.02, 0.004], seg=28, per=10), PURPLE, "head", fuse=False)

C0 = np.array([0.0, 0.02, 0.78])
def brim_at(u, v):
    phi = u * 2 * math.pi * 1.012 + math.pi                 # the seam behind, its ends overlapping; the front at 0
    vv = 0.02 + 0.96 * v
    rim = 0.3 * (1 + 0.045 * np.sin(5 * phi + 0.6) + 0.025 * np.sin(9 * phi))
    r = 0.075 + (rim - 0.075) * vv
    zr = 0.6 + 0.14 * np.clip(-np.cos(phi + math.pi), 0, 1) ** 1.2 + 0.012 * np.sin(7 * phi)  # the front raised
    zr = zr + 0.02 * np.cos(phi)
    sag = (1 - (1 - vv) ** 1.6)                             # a soft droop, flatter near the crown
    z = C0[2] + (zr - C0[2]) * sag - TH * 1.1 * smoothstep(0.985, 1.0, u)   # the end tucked under the start
    x = C0[0] + r * np.sin(phi)
    y = C0[1] - r * np.cos(phi)
    return np.stack([x, y, z], axis=-1)
NU, NV, TH = 120, 22, 0.016
BV, BF = sheet(brim_at, NU, NV, TH)
half = len(BV) // 2
lower_first = BV[:half, 2].mean() < BV[half:, 2].mean()
bc = np.vstack([np.tile(U_ if lower_first else P_, (half, 1)), np.tile(P_ if lower_first else U_, (half, 1))])
gadd("brim", (BV, BF), bc, "head")

# Ragged tufts: round the crown's base, up the crown's +X side, and from under the brim at the sides
tuft("tuftbaseL", (-0.085, 0.04, 0.8), (-0.75, 0.15, 0.65), 3, 0.11, 0.022)
tuft("tuftbaseR", (0.085, 0.04, 0.81), (0.8, 0.2, 0.55), 4, 0.13, 0.024)
tuft("tuftback", (0.0, 0.11, 0.79), (0.0, 0.9, 0.45), 3, 0.1, 0.022, flat=(1, 0, 0))
tuft("tufttip", (0.1, 0.066, 0.99), (1, 0.25, -0.2), 3, 0.075, 0.014, spread=0.55)
tuft("tuftcone", (0.04, 0.05, 0.9), (0.7, 0.3, 0.6), 2, 0.07, 0.016)
tuft("tuftrimR", (0.27, 0.05, 0.62), (1, 0.1, -0.1), 3, 0.09, 0.02, flat=(0, 0, 1))
tuft("tuftrimL", (-0.27, 0.05, 0.6), (-1, 0.1, -0.35), 3, 0.09, 0.02, flat=(0, 0, 1))
tuft("hairR", (0.12, 0.06, 0.62), (0.45, 0.2, -1), 3, 0.11, 0.022, flat=(0, 1, 0))
tuft("hairL", (-0.12, 0.06, 0.62), (-0.45, 0.2, -1), 3, 0.11, 0.022, flat=(0, 1, 0))


# ---- the cloak: from the neck down, flaring, a hem of points, side panels edged in pink, two long wavy tails
TAILS = (0.85, -0.85)                    # the tails' ways, in radians from the front toward +X
def cloak_at(u, v, out_by=0.0):
    phi = (u - 0.5) * 2 * math.pi * 1.012                    # the seam behind, its ends overlapping; the front at u 0.5
    phi = (phi + math.pi) % (2 * math.pi) - math.pi
    wt = sum(np.exp(-((phi - a) / 0.2) ** 2) for a in TAILS)          # how much of a tail a place is
    ws = np.exp(-((np.abs(phi) - math.pi / 2) / 0.45) ** 2)           # how much of a side panel
    teeth = 0.035 * tri(phi * 11 / (2 * math.pi) + 0.25)
    L = 0.33 + teeth * (1 - wt) + 0.21 * wt + 0.04 * ws - 0.03 * np.clip(np.cos(phi), -1, 0) ** 2
    r = (0.062 + 0.15 * v ** 1.25) * (1 + 0.35 * ws * v ** 1.5) * (1 - 0.25 * wt * smoothstep(0.6, 1.0, v)) + out_by - CT * 1.1 * smoothstep(0.985, 1.0, u)
    z = 0.585 - L * v
    # the tails wave sideways toward their tips
    wave = wt * 0.06 * np.sin(v * 15 + 1.0 * np.sign(phi)) * smoothstep(0.45, 1.0, v)
    ph2 = phi + wave / np.maximum(r, 0.05)
    x = r * np.sin(ph2)
    y = -r * np.cos(ph2) + 0.01
    return np.stack([x, y, z], axis=-1)
CU, CV, CT = 160, 40, 0.012
CVt, CF = sheet(cloak_at, CU, CV, CT)
uu, vv = np.meshgrid(np.linspace(0, 1, CU), np.linspace(0, 1, CV), indexing="ij")
phi = (uu - 0.5) * 2 * math.pi * 1.012
wt = sum(np.exp(-((phi - a) / 0.2) ** 2) for a in TAILS)
ws = np.exp(-((np.abs(phi) - math.pi / 2) / 0.5) ** 2)
edge = 0.3 * wt + 0.14 * ws * (1 - wt)                                 # how far up from the hem the pink comes
pk = smoothstep(1 - edge - 0.0001, 1 - edge + 0.06, vv) * (edge > 0.02)
deep = smoothstep(0.93, 1.0, vv) * np.clip(wt * 1.4, 0, 1)
col = P_[None, None] * (1 - pk[..., None]) + K_[None, None] * pk[..., None]
col = col * (1 - deep[..., None]) + D_[None, None] * deep[..., None]
col = col.reshape(-1, 3)
gadd("cloak", (CVt, CF), np.vstack([col, col]), "body")

# Three red-pink oval gems across the chest, the middle one a little lower, each with a darker rim and a glint
for j, (a, dz, s) in enumerate([(-0.6, 0.0, 1.0), (0.0, -0.03, 1.0), (0.6, 0.0, 1.0)]):
    v = (0.585 - (0.49 + dz)) / 0.33
    p = cloak_at(np.array(0.5 + a / (2 * math.pi * 1.012)), np.array(v), CT / 2 + 0.002)
    p = np.array(p, float)
    nrm = unit(math.sin(a), -math.cos(a), 0.12)
    rot = (0, 0, math.degrees(a))
    k.add(f"gemrim{j}", ell(p - nrm * 0.004, (0.026 * s, 0.01, 0.045 * s), rot=rot), GEMDEEP, "body", fuse=False,
          surface="shell")
    k.add(f"gem{j}", ell(p + nrm * 0.0, (0.021 * s, 0.012, 0.039 * s), rot=rot), GEM, "body", fuse=False,
          surface="shell")
    k.add(f"gemshine{j}", ell(p + nrm * 0.011 + np.array([-0.005, 0, 0.016]), (0.005, 0.003, 0.01), rot=rot, sub=2),
          GEMLIGHT, "body", fuse=False, surface="shell")

k.build(voxel=0.0055, smooth=3)
recolour(k.ob)

# The eyes: Misdreavus's yellow eyes with red irises toward the nose, a dark pupil and a glint, their lid straight
# and sloping hard down toward the nose: an angry look. u runs outward, v up.
YEL, IRIS, PUP, LID = srgb("#F6CE3A"), srgb("#D8303C"), srgb("#3A0C12"), srgb(PURPLE)
def eye(u, v):
    c = np.tile(YEL, (len(u), 1))
    lid = 0.18 + 0.42 * u
    c[((u + 0.2) / 0.48) ** 2 + ((v + 0.12) / 0.62) ** 2 < 1] = IRIS
    c[((u + 0.24) / 0.19) ** 2 + ((v + 0.12) / 0.32) ** 2 < 1] = PUP
    c[((u + 0.02) / 0.12) ** 2 + ((v - 0.02) / 0.13) ** 2 < 1] = (1, 1, 1)
    c[u * u + v * v > 0.8] = INK
    c[v > lid + 0.13] = LID
    c[np.abs(v - lid) <= 0.13] = INK
    return c
de = unit(0.5, -0.84, 0.2)
def eyes(state):
    k.decal(H + de * 0.1, de, (0.04, 0.032), eye_look(state, eye, LID, INK), eps=0.005, rings=20, segs=56,
            mirror=True)

k.eye_states(eyes)
k.export(out)
