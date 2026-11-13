# Ponyta sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/ponyta.py -- <the export's Ponyta.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

CREAM, HOOF, PINK, NOSTRIL = "#F8EDBE", "#7E7AA0", "#E9A3A8", "#9A8670"
ORANGE, DEEP, YELLOW = "#FF7A22", "#F2521E", "#FFD23A"
H = np.array([0, -0.335, 0.865])        # the head's centre

# A rounded barrel of a body on slender legs
k.add("barrel", ell((0, 0.02, 0.535), (0.125, 0.24, 0.125)), CREAM, "body")
k.add("chest", ell((0, -0.15, 0.54), (0.115, 0.1, 0.13)), CREAM, "body")
k.add("rump", ell((0, 0.18, 0.55), (0.12, 0.1, 0.12)), CREAM, "body")
# A long neck rising to a small head with a tapering muzzle
k.add("neck", tube([(0, -0.17, 0.57), (0, -0.25, 0.7), (0, -0.3, 0.83)], [0.1, 0.082, 0.07]), CREAM, "head")
k.add("head", ell(H, (0.08, 0.108, 0.08)), CREAM, "head")
k.add("muzzle", ell((0, -0.425, 0.822), (0.057, 0.075, 0.055)), CREAM, "head")
# Pointed ears, pink within
k.add("ear", blade([(0.04, -0.3, 0.9), (0.07, -0.297, 0.955), (0.105, -0.29, 1.005)], [0.03, 0.022, 0.003],
                   (0, 1, 0), 0.5), CREAM, "ear{s}", mirror=True)

# Legs: slender, a little knee, grey hooves
for name, y, bone in (("fore", -0.17, "leg{s}"), ("hind", 0.21, "hind{s}")):
    knee = -0.008 if name == "fore" else 0.02
    k.add(name, tube([(0.08, y, 0.47), (0.083, y + knee, 0.26), (0.083, y, 0.12), (0.083, y - 0.005, 0.05)],
                     [0.058, 0.034, 0.03, 0.031]), CREAM, bone, mirror=True)
    k.add(name + "hoof", ell((0.083, y - 0.01, 0.031), (0.036, 0.042, 0.027), deform=lambda V: V * np.array(
        [1, 1, 1]) * (1 + 0.15 * np.clip(-V[:, 2:3], 0, 1))), HOOF, bone, mirror=True, surface="shell")

# Flames: tongues that sway, taper to a point and flick, their edges ragged; orange outside with a yellow heart
def frame(d, side):
    d = np.array(d, float); d /= np.linalg.norm(d)
    s = np.array(side, float); s -= d * np.dot(s, d); s /= np.linalg.norm(s)
    return np.stack([s, np.cross(d, s), d], axis=1)
count = [0]
def tongue(base, d, length, width, side=(0, 1, 0), flick=0.35, thick=0.38, rag=0.24, waves=1.6, root=0.5, sub=None):
    """
    A tongue of flame from base along d: a sway one way low and its tip flicked toward side, each edge waving on a
    phase of its own (a ragged outline, not a leaf's), tapering to a fine point.
    """
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
FULL = 1.35            # every tongue's width is written thin and filled out by this
groups = {}            # (bone, colour, glow) -> tongues: one piece and one glowing material a group, not a tongue
def gather(shape, color, bone, glow):
    groups.setdefault((bone, color, glow), []).append(shape)
def flame(base, d, length, width, bone, side=(0, 1, 0), flick=0.35, heart=True, color=ORANGE, glow="#FF7A2A",
          lick=True):
    width = width * FULL
    b = np.array(base, float); dd = np.array(d, float) / np.linalg.norm(d)
    gather(tongue(b, dd, length, width, side, flick), color, bone, glow)
    if lick and length >= 0.09:
        # A small lick forking off the far edge, curling out: a flame's outline is never one smooth curve
        su = frame(dd, side)[:, 0]
        dl = dd - 0.75 * su; dl /= np.linalg.norm(dl)
        gather(tongue(b + dd * length * 0.32 - su * width * 0.35, dl, length * 0.42, width * 0.42, -su, flick * 0.8,
                      sub=3), color, bone, glow)
    if heart:
        # The yellow heart, a thin flame of its own laid on each face of the tongue, so it shows from either side
        ny = frame(dd, side)[:, 1]
        for sg in (1, -1):
            gather(tongue(b + dd * 0.004 + ny * sg * width * 0.24, dd, length * 0.6, width * 0.56, side, flick * 0.5,
                          thick=0.3, rag=0.16, root=0.2, sub=3), YELLOW, bone, "#FFD860")
def back(base, d, length, width, bone, side, flick=0.45):
    """A deeper tongue behind the others, for the tips' red-orange."""
    flame(base, d, length, width, bone, side, flick, heart=False, color=DEEP, glow="#FF5A22")

# The forelock: a flame rising from the crown, fanning and sweeping back, kept low enough that the body reads large
for x, d, L, w, f in [(0.0, (0, 0.85, 1), 0.15, 0.085, 0.6), (0.0, (0, -0.1, 1), 0.085, 0.06, 0.5),
                      (0.015, (0.4, 0.45, 1), 0.11, 0.06, 0.5), (-0.015, (-0.4, 0.45, 1), 0.11, 0.06, 0.5),
                      (0.01, (0.25, 1, 0.6), 0.15, 0.06, 0.4), (-0.01, (-0.25, 1, 0.6), 0.15, 0.06, 0.4)]:
    flame((x, -0.33, 0.905), d, L, w, "mane", side=(0, 1, 0.2), flick=f)
back((0, -0.31, 0.91), (0, 1, 0.55), 0.16, 0.065, "mane", (0, 1, 0))
# The mane down the back of the neck, tongues licking back and out to both sides
for i, t in enumerate(np.linspace(0, 1, 6)):
    p = np.array([0, -0.28, 0.85]) * (1 - t) + np.array([0, -0.1, 0.63]) * t
    flame(p, (0, 1, 0.75 - 0.3 * t), 0.15 - 0.04 * t, 0.05 - 0.01 * t, "mane", side=(0, 0, 1), flick=0.35)
    for sx in (1, -1):
        flame(p + np.array([0, 0.005, -0.015]), (sx * 0.6, 0.9, 0.55), 0.1 - 0.02 * t, 0.036, "mane", side=(0, 0, 1),
              flick=0.3, lick=False)
# The tail: a great flame streaming back and up from the rump, a fan of tongues
for d, L, w, sd in [((0, 1, 0.4), 0.36, 0.09, (0, 0, 1)), ((0, 0.6, 1), 0.28, 0.075, (0, 1, 0)),
                    ((0.5, 1, 0.5), 0.24, 0.065, (0, 0, 1)), ((-0.5, 1, 0.5), 0.24, 0.065, (0, 0, 1)),
                    ((0.3, 1, -0.05), 0.2, 0.055, (0, 0, 1)), ((-0.3, 1, -0.05), 0.2, 0.055, (0, 0, 1)),
                    ((0, 1, -0.3), 0.16, 0.05, (0, 0, 1))]:
    flame((0, 0.26, 0.6), d, L, w, "tail", side=sd, flick=0.45)
back((0, 0.27, 0.62), (0, 1, 0.65), 0.29, 0.09, "tail", (0, 0, 1), flick=0.55)
# Flames low on the backs of the legs: a cluster behind the knee, a small one behind the fetlock, licking back and up
for y, bone, knee in ((-0.17, "leg{s}", -0.008), (0.21, "hind{s}", 0.02)):
    for s, side in ((1, "R"), (-1, "L")):
        b = bone.format(s=side)
        flame((0.083 * s, y + knee + 0.02, 0.3), (0.2 * s, 1, 0.45), 0.14, 0.05, b, side=(0, 0, 1), flick=0.4)
        flame((0.083 * s, y + knee + 0.022, 0.235), (0.25 * s, 1, 0.15), 0.12, 0.045, b, side=(0, 0, 1), flick=0.35)
        flame((0.083 * s, y + 0.018, 0.135), (0.2 * s, 1, 0.3), 0.085, 0.035, b, side=(0, 0, 1), flick=0.35, heart=False)

# Each group of tongues one piece
for i, ((bone, color, glow), shapes) in enumerate(groups.items()):
    V, F, off = [], [], 0
    for sv, sf in shapes:
        V.append(sv); F.extend([[j + off for j in f] for f in sf]); off += len(sv)
    k.add(f"flame{i}", (np.vstack(V), F), color, bone, fuse=False, glow=glow)

# Nostrils and a line of a mouth
k.decal((0.022, -0.488, 0.835), unit(0.25, -1, 0.1), (0.008, 0.006), NOSTRIL, eps=0.003, rings=4, segs=20, mirror=True)
k.decal((0.035, -0.47, 0.795), unit(0.6, -1, -0.3), (0.025, 0.004), NOSTRIL, eps=0.003, rings=3, segs=20, mirror=True)
# The ears' pink linings
k.decal((0.072, -0.315, 0.952), unit(0.25, -1, 0.05), (0.013, 0.034), PINK, up=unit(0.5, 0, 0.85), eps=0.003, rings=6, segs=28,
        mirror=True)

# The eyes: large and dark brown, a white glint, set on the sides of the head
DARK, BROWN, CI, CC = srgb("#1E1612"), srgb("#4A2A1C"), srgb("#2A201A"), srgb(CREAM)
def eye(u, v):
    c = np.tile(DARK, (len(u), 1))
    c[((u + 0.0) / 0.7) ** 2 + ((v + 0.25) / 0.55) ** 2 < 1] = BROWN
    c[((u + 0.0) / 0.45) ** 2 + ((v + 0.1) / 0.5) ** 2 < 1] = DARK
    c[((u + 0.25) / 0.2) ** 2 + ((v - 0.35) / 0.22) ** 2 < 1] = (1, 1, 1)
    return c
de = unit(0.78, -0.55, 0.15)
E = H + np.array([0, -0.03, 0.01]) + de * 0.072
def eyes(state):
    k.decal(E, de, (0.028, 0.034), eye_look(state, eye, CC, CI), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.007)
k.eye_states(eyes)
k.export(out)
