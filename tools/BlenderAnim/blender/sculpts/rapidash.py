# Rapidash sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/rapidash.py -- <the export's Rapidash.glb> <out.glb>
# Grown from ponyta.py: the same cream, hooves and flames (ragged tongues with yellow hearts on both faces), a horn,
# a longer neck and longer legs, a mane and tail streaming back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

CREAM, HOOF, PINK, NOSTRIL = "#F8EDBE", "#8A86A6", "#E9A3A8", "#9A8670"
HORN = "#F1E3B6"
ORANGE, DEEP, YELLOW = "#FF7A22", "#F2521E", "#FFD23A"
H = np.array([0, -0.37, 0.878])         # the head's centre

# A slimmer barrel than Ponyta's, a deep chest and a rounded rump
k.add("barrel", ell((0, 0.025, 0.555), (0.108, 0.235, 0.108)), CREAM, "body")
k.add("chest", ell((0, -0.16, 0.565), (0.1, 0.095, 0.12)), CREAM, "body")
k.add("rump", ell((0, 0.19, 0.57), (0.106, 0.1, 0.108)), CREAM, "body")
# A long arched neck to a long head with a muzzle tapering down
k.add("neck", tube([(0, -0.15, 0.6), (0, -0.24, 0.725), (0, -0.3, 0.82), (0, -0.335, 0.87)],
                   [0.088, 0.07, 0.058, 0.054]), CREAM, "head")
k.add("head", ell(H, (0.066, 0.095, 0.066)), CREAM, "head")
k.add("muzzle", ell((0, -0.462, 0.838), (0.047, 0.085, 0.046), rot=(14, 0, 0)), CREAM, "head")
# Pointed ears, pink within
k.add("ear", blade([(0.033, -0.33, 0.915), (0.052, -0.327, 0.958), (0.07, -0.32, 0.995)], [0.025, 0.019, 0.003],
                   (0, 1, 0), 0.5), CREAM, "ear{s}", mirror=True)
# The horn: one long point from the forehead, loose to keep its point
k.add("horn", tube([(0, -0.398, 0.918), (0, -0.398, 0.975), (0, -0.392, 1.035)], [0.02, 0.012, 0.0015]), HORN,
      "head", fuse=False)

# Legs: long and slender, a knee, grey hooves
for name, y, bone, knee in (("fore", -0.19, "leg{s}", -0.008), ("hind", 0.235, "hind{s}", 0.03)):
    k.add(name, tube([(0.074, y, 0.5), (0.08, y + knee, 0.27), (0.08, y, 0.12), (0.08, y - 0.005, 0.05)],
                     [0.052, 0.029, 0.024, 0.026]), CREAM, bone, mirror=True)
    k.add(name + "hoof", ell((0.08, y - 0.01, 0.029), (0.033, 0.039, 0.026), deform=lambda V: V * (
        1 + 0.15 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone, mirror=True, surface="shell")
k.add("thigh", ell((0.07, 0.21, 0.47), (0.052, 0.085, 0.1)), CREAM, "hind{s}", mirror=True)

# Flames, as Ponyta's: tongues that sway, taper and flick, their edges ragged; orange outside, a yellow heart
def frame(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)
count = [0]
def tongue(base, d, length, width, side=(0, 1, 0), flick=0.35, thick=0.38, rag=0.24, waves=1.6, root=0.5, sub=None):
    count[0] += 1
    ph = 2.3 * count[0]
    def shape(V):
        V = V.copy()
        t = np.clip((V[:, 2] + 1) / 2, 0, 1)
        w = (1 - t ** 1.1) ** 1.15 * (root + (1.55 - root) * np.sqrt(t))
        s = np.where(V[:, 0] >= 0, 1.0, -1.0)
        edge = 1 + rag * np.sin(2 * math.pi * waves * t + ph + s * 2.1) * smoothstep(0.1, 0.35, t)
        V[:, 0] *= w * edge; V[:, 1] *= w
        V[:, 0] += flick * (2.1 * t ** 2.4 - 0.55 * np.sin(math.pi * t))
        return V
    V, F = ell((0, 0, 0), (width, width * thick, length / 2), deform=shape, sub=sub or (4 if length > 0.13 else 3))
    return V @ frame(d, side).T + np.array(base, float) + frame(d, side)[:, 2] * length / 2, F
FULL = 1.35
groups = {}
def gather(shape, color, bone, glow):
    groups.setdefault((bone, color, glow), []).append(shape)
def flame(base, d, length, width, bone, side=(0, 1, 0), flick=0.35, heart=True, color=ORANGE, glow="#FF7A2A",
          lick=True):
    width = width * FULL
    b = np.array(base, float); dd = np.array(d, float) / np.linalg.norm(d)
    gather(tongue(b, dd, length, width, side, flick), color, bone, glow)
    if lick and length >= 0.09:
        su = frame(dd, side)[:, 0]
        dl = dd - 0.75 * su; dl /= np.linalg.norm(dl)
        gather(tongue(b + dd * length * 0.32 - su * width * 0.35, dl, length * 0.42, width * 0.42, -su, flick * 0.8,
                      sub=3), color, bone, glow)
    if heart:
        ny = frame(dd, side)[:, 1]
        for sg in (1, -1):
            gather(tongue(b + dd * 0.004 + ny * sg * width * 0.24, dd, length * 0.6, width * 0.56, side, flick * 0.5,
                          thick=0.3, rag=0.16, root=0.2, sub=3), YELLOW, bone, "#FFD860")
def back(base, d, length, width, bone, side, flick=0.45):
    flame(base, d, length, width, bone, side, flick, heart=False, color=DEEP, glow="#FF5A22")

# The mane: from behind the ears down the neck to the withers, a great fan of tongues streaming back, kept low over
# the head so that the body reads large
for x, d, L, w, f in [(0.0, (0, 1, 0.12), 0.24, 0.07, 0.25), (0.012, (0.35, 1, 0.1), 0.17, 0.05, 0.25),
                      (-0.012, (-0.35, 1, 0.1), 0.17, 0.05, 0.25)]:
    flame((x, -0.315, 0.925), d, L, w, "mane", side=(0, 0, 1), flick=f)
back((0, -0.3, 0.91), (0, 1, 0.08), 0.26, 0.06, "mane", (0, 0, 1), flick=0.25)
for i, t in enumerate(np.linspace(0, 1, 7)):
    p = np.array([0, -0.3, 0.9]) * (1 - t) + np.array([0, -0.08, 0.66]) * t
    flame(p, (0, 1, 0.2 - 0.1 * t), 0.28 - 0.04 * t, 0.06 - 0.012 * t, "mane", side=(0, 0, 1), flick=0.25)
    for sx in (1, -1):
        flame(p + np.array([0, 0.005, -0.015]), (sx * 0.55, 1, 0.3), 0.14 - 0.02 * t, 0.038, "mane", side=(0, 0, 1),
              flick=0.3, lick=False)
    if i % 2 == 1:
        back(p + np.array([0, 0.01, 0.005]), (0, 1, 0.15), 0.3 - 0.04 * t, 0.05, "mane", (0, 0, 1), flick=0.3)
# along the back to the middle of it
for t in np.linspace(0, 1, 3):
    p = np.array([0, -0.04, 0.66]) * (1 - t) + np.array([0, 0.06, 0.66]) * t
    flame(p, (0, 1, 0.22), 0.18, 0.045, "body", side=(0, 0, 1), flick=0.35)

# The tail: a long flame streaming back from the rump
for d, L, w, sd in [((0, 1, 0.1), 0.42, 0.09, (0, 0, 1)), ((0, 1, 0.4), 0.3, 0.07, (0, 1, 0)),
                    ((0.45, 1, 0.3), 0.28, 0.065, (0, 0, 1)), ((-0.45, 1, 0.3), 0.28, 0.065, (0, 0, 1)),
                    ((0.3, 1, -0.2), 0.25, 0.055, (0, 0, 1)), ((-0.3, 1, -0.2), 0.25, 0.055, (0, 0, 1)),
                    ((0, 1, -0.45), 0.2, 0.05, (0, 0, 1))]:
    flame((0, 0.27, 0.6), d, L, w, "tail", side=sd, flick=0.45)
back((0, 0.28, 0.62), (0, 1, 0.2), 0.36, 0.09, "tail", (0, 0, 1), flick=0.4)

# Flames at the fetlocks, streaming back, and a smaller one behind each knee
for y, bone, knee in ((-0.19, "leg{s}", -0.008), (0.235, "hind{s}", 0.03)):
    for s, side in ((1, "R"), (-1, "L")):
        b = bone.format(s=side)
        flame((0.08 * s, y + 0.016, 0.115), (0.2 * s, 1, 0.25), 0.15, 0.045, b, side=(0, 0, 1), flick=0.4)
        flame((0.08 * s, y + 0.018, 0.09), (0.3 * s, 1, 0.05), 0.1, 0.035, b, side=(0, 0, 1), flick=0.35, heart=False)
        flame((0.08 * s, y + knee + 0.02, 0.29), (0.2 * s, 1, 0.3), 0.1, 0.035, b, side=(0, 0, 1), flick=0.35)

for i, ((bone, color, glow), shapes) in enumerate(groups.items()):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
    k.add(f"flame{i}", (np.vstack(V), F), color, bone, fuse=False, glow=glow)

# Nostrils and a line of a mouth
k.decal((0.02, -0.535, 0.83), unit(0.25, -1, 0.0), (0.008, 0.006), NOSTRIL, eps=0.003, rings=4, segs=20, mirror=True)
k.decal((0.03, -0.51, 0.79), unit(0.6, -1, -0.4), (0.026, 0.004), NOSTRIL, eps=0.003, rings=3, segs=20, mirror=True)
# The ears' pink linings
k.decal((0.05, -0.342, 0.952), unit(0.25, -1, 0.05), (0.012, 0.032), PINK, up=unit(0.45, 0, 0.9), eps=0.003, rings=6,
        segs=28, mirror=True)

# The eyes: almonds set on the head's sides, red with a dark lid drawn over their tops (u runs toward the back)
CC, CI = srgb(CREAM), srgb("#2A1A16")
RED, REDD, PUP = srgb("#E03A4E"), srgb("#8E1A2C"), srgb("#2A0E12")
def top(u):
    return 0.3 + 0.05 * u - 0.16 * u * u
def bottom(u):
    return -0.42 + 0.3 * u * u
def eye(u, v):
    c = np.tile(CC, (len(u), 1))
    inn = (np.abs(u) < 0.9) & (v < top(u)) & (v > bottom(u))
    c[inn] = srgb("#FBF1EE")
    ir = ((u + 0.05) / 0.42) ** 2 + ((v + 0.06) / 0.48) ** 2
    c[inn & (ir < 1)] = REDD
    c[inn & (ir < 0.65)] = RED
    c[inn & (((u + 0.05) / 0.15) ** 2 + ((v + 0.08) / 0.25) ** 2 < 1)] = PUP
    c[inn & (((u + 0.22) / 0.13) ** 2 + ((v - 0.08) / 0.13) ** 2 < 1)] = (1, 1, 1)
    c[(np.abs(v - top(u)) < 0.11) & (np.abs(u) < 0.95)] = CI
    c[(np.abs(v - bottom(u)) < 0.05) & (np.abs(u) < 0.8)] = CI
    return c
de = unit(0.78, -0.55, 0.15)
E = H + np.array([0, -0.03, 0.012]) + de * 0.06
def eyes(state):
    k.decal(E, de, (0.03, 0.026), eye_look(state, eye, CC, CI), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
