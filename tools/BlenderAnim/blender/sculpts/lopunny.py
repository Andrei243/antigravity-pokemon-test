# Lopunny sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/lopunny.py -- <the export's Lopunny.glb> <out.glb>
# Grown from buneary.py: the same brown, cream and pink, the same fleece of fused balls.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, boxy, eye_look, polygon, args

src, out = args()
k = Kit(src, coat="fur")

BROWN, CREAM, PINK, INK = "#A5674A", "#F3DE98", "#E8909E", "#1A1414"
H = np.array([0, -0.01, 0.815])          # the head's centre
HR = np.array([0.088, 0.08, 0.083])


def puff(name, c, r, bone, n=8, tilt=0.0, squash=(1, 1, 1)):
    """A cloud of fleece: a core and fused balls round it."""
    c = np.array(c, float); sq = np.array(squash, float)
    k.add(name, ell(c, np.array((r * 0.78, r * 0.74, r * 0.74)) * sq), CREAM, bone)
    g = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + 0.5) / n
        rr = math.sqrt(1 - z * z)
        a = g * i + tilt
        d = np.array([rr * math.cos(a), rr * math.sin(a), z * 0.8]) * sq
        k.add(f"{name}{i}", ell(c + d * r * 0.48, np.array((r * 0.5, r * 0.5, r * 0.48)) * sq), CREAM, bone)


# The head: round, a little narrower at the chin, a small muzzle
def chin(V):
    V = boxy(2.2)(V)
    V = V.copy()
    V[:, 0] *= 1 - 0.14 * np.clip(-V[:, 2], 0, 1)
    return V
k.add("head", ell(H, HR, deform=chin), BROWN, "head")
k.add("muzzle", ell((0, -0.048, 0.788), (0.048, 0.045, 0.034)), BROWN, "head")
k.add("neck", tube([(0, 0.0, 0.66), (0, -0.004, 0.75)], [0.046, 0.042]), BROWN, "head")

# A slender body: a chest, a narrow waist, rounder hips
k.add("chest", ell((0, 0.005, 0.625), (0.08, 0.062, 0.075)), BROWN, "body")
k.add("waist", ell((0, 0.01, 0.54), (0.058, 0.05, 0.062)), BROWN, "body")
k.add("hips", ell((0, 0.012, 0.465), (0.092, 0.075, 0.062)), BROWN, "body")

# Long legs: brown thighs, cream fleece from the shin down, a tuft at the fleece's top, pointed cream feet
k.add("leg", tube([(0.06, 0.01, 0.47), (0.071, 0.0, 0.34), (0.075, -0.012, 0.23), (0.075, 0.0, 0.12),
                   (0.073, 0.0, 0.055)], [0.056, 0.048, 0.035, 0.03, 0.027]), BROWN, "leg{s}", mirror=True)
k.add("boot", ell((0.075, 0.0, 0.13), (0.044, 0.044, 0.085)), CREAM, "leg{s}", mirror=True)
k.add("boottuft", blade([(0.1, 0.01, 0.18), (0.125, 0.03, 0.225), (0.145, 0.05, 0.265)], [0.022, 0.016, 0.003],
                        (0, 1, 0), 0.5), CREAM, "leg{s}", mirror=True)
def toe(V):
    V = V.copy()
    f = np.clip(-V[:, 1], 0, 1)
    V[:, 0] *= 1 - 0.55 * f ** 1.5; V[:, 2] *= 1 - 0.4 * f ** 1.5
    return V
k.add("foot", ell((0.074, -0.045, 0.026), (0.036, 0.075, 0.028), deform=toe), CREAM, "leg{s}", mirror=True)
# the shins cream with a ragged top, kept to the legs by place
def shins(P):
    ax = np.abs(P[:, 0]) - 0.075
    a = np.arctan2(ax, -P[:, 1])
    return inside(P[:, 2] - (0.205 + 0.016 * tri(a * 1.4)), 0.004) * inside(np.abs(P[:, 0]) - 0.16, 0.004)
k.paint(shins, CREAM, on=["leg", "boot", "foot"])

# Arms bent to hold the hands before the chest; a great cloud of fleece at each wrist, the two meeting in the middle
k.add("arm", tube([(0.085, -0.005, 0.655), (0.11, -0.025, 0.6), (0.1, -0.06, 0.565), (0.06, -0.09, 0.58)],
                  [0.03, 0.026, 0.025, 0.024]), BROWN, "arm{s}", mirror=True)
for s, side in ((1, "R"), (-1, "L")):
    puff(f"cuff{side}", (0.055 * s, -0.088, 0.585), 0.06, f"arm{side}", n=9, tilt=0.4 * s)
# The hands rest on the fleece, three fingers curled over it: loose, to keep their brown crisp
k.add("hand", ell((0.032, -0.133, 0.627), (0.026, 0.02, 0.02)), BROWN, "arm{s}", fuse=False, mirror=True)
for i, x in enumerate((0.014, 0.032, 0.05)):
    k.add(f"finger{i}", tube([(x, -0.146, 0.628), (x + 0.002, -0.153, 0.6)], [0.0095, 0.0075]), BROWN, "arm{s}",
          fuse=False, mirror=True)

# A small round tail
k.add("tail", ell((0, 0.092, 0.47), (0.038, 0.034, 0.034)), BROWN, "tail")

# The ears (as both references draw them): each rises from the head's top as a brown point lined pink, and from the
# same root its long folded part falls past the waist into a great cream cuff of fleece
EAR = [(0.042, 0.02, 0.87), (0.052, 0.022, 0.95), (0.062, 0.025, 1.035)]
k.add("ear", blade(EAR, [0.03, 0.033, 0.006], (0, 1, 0), 0.6), BROWN, "ear{s}", mirror=True)
# the lining painted on the ear's front, inside its edge, so it never shows from the side or behind
EA, EB = np.array(EAR[0]), np.array(EAR[2])
def lining(P):
    ax = (EB - EA) / np.linalg.norm(EB - EA)
    Q = P.copy(); Q[:, 0] = np.abs(Q[:, 0])
    t = np.clip((Q - EA) @ ax / np.linalg.norm(EB - EA), 0, 1)
    off = Q - (EA + np.outer(t * np.linalg.norm(EB - EA), ax))
    half = 0.017 * np.sin(np.pi * np.clip((t - 0.06) / 0.86, 0, 1)) ** 0.7
    return (inside(np.abs(off[:, 0]) - half, 0.003) * inside(off[:, 1] + 0.006, 0.003)
            * inside(t - 0.92, 0.01))
k.paint(lining, PINK, on=["ear"])
# The long ear falls from the same root: out over the head's top, then down past the waist into a great cream cuff;
# flat, wider than thick, so it reads as an ear and not an arm
k.add("lobe", blade([(0.045, 0.026, 0.875), (0.1, 0.03, 0.885), (0.16, 0.03, 0.84), (0.2, 0.026, 0.76),
                     (0.228, 0.02, 0.68)], [0.028, 0.032, 0.036, 0.041, 0.046], (0, 1, 0), 0.55),
      BROWN, "head", mirror=True)
def mitten(V):
    V = V.copy()
    w = 1 + 0.32 * np.clip(-V[:, 2], 0, 1)
    V[:, 0] *= w; V[:, 1] *= w
    return V
k.add("lobecuff", ell((0.245, 0.015, 0.515), (0.082, 0.046, 0.195), deform=mitten), CREAM, "head", mirror=True)
for i, a in enumerate(np.radians([-150, -110, -70, -30, 10, 50, 90, 130])):
    # scallops round the cuff's lower edge, none on its inner side by the thighs
    if abs(math.cos(a) + 1) < 0.25:
        continue
    k.add(f"lobepuff{i}", ell((0.245 + 0.068 * math.cos(a), 0.015 + 0.052 * math.sin(a), 0.36 + 0.012 * (i % 2)),
                              (0.042, 0.04, 0.045)), CREAM, "head", mirror=True)
k.add("lobepuffend", ell((0.245, 0.015, 0.34), (0.06, 0.048, 0.045)), CREAM, "head", mirror=True)

# Cream tufts over the eyes, rising up and out before the ears
k.add("brow", blade([(0.032, -0.066, 0.866), (0.074, -0.056, 0.912), (0.115, -0.042, 0.958)], [0.019, 0.023, 0.003],
                    (0.15, 1, 0.1), 0.45), CREAM, "head", fuse=False, mirror=True)

# A small pink nose and a small calm mouth
NOSE = polygon([(-1, 0.7), (1, 0.7), (0, -1)])
k.decal((0, -0.09, 0.797), unit(0, -1, -0.15), (0.009, 0.0075), PINK, eps=0.004, outline=NOSE, rings=6, segs=24)
CB, CI = srgb(BROWN), srgb(INK)
CM = srgb("#4A2418")
def mouth(u, v):
    c = np.tile(CB, (len(u), 1))
    c[(np.abs(v - (-0.1 + 0.45 * u * u)) < 0.22) & (np.abs(u) < 0.8)] = CM
    return c
k.decal((0, -0.088, 0.776), unit(0, -1, -0.35), (0.018, 0.011), mouth, eps=0.004, rings=8, segs=36)

# The eyes: pink-red, a dark lid drawn low and calm across them with a flick at the outer corner like eyeliner
IRIS, IRISD, PUP, SCL = srgb("#E5506E"), srgb("#A02642"), srgb("#38101A"), srgb("#FBEFF0")
def lidline(u):
    return 0.3 - 0.32 * u * u
def eye(u, v):
    c = np.tile(CB, (len(u), 1))
    e = (u / 0.64) ** 2 + ((v + 0.1) / 0.58) ** 2
    inn = (e < 1) & (v < lidline(u))
    c[inn] = SCL
    ir = ((u + 0.03) / 0.5) ** 2 + ((v + 0.1) / 0.56) ** 2
    c[inn & (ir < 1)] = IRISD
    c[inn & (ir < 0.72)] = IRIS
    c[inn & (((u + 0.03) / 0.17) ** 2 + ((v + 0.12) / 0.26) ** 2 < 1)] = PUP
    c[inn & (((u + 0.2) / 0.13) ** 2 + ((v - 0.06) / 0.13) ** 2 < 1)] = (1, 1, 1)
    c[(e < 1) & (e > 0.8) & (v < -0.1)] = CI                              # the lower rim
    c[(np.abs(v - lidline(u) - 0.03) < 0.1) & (np.abs(u) < 0.64)] = CI  # the lid
    wing = (u > 0.5) & (u < 0.9)
    t = (u - 0.5) / 0.4
    c[wing & (np.abs(v - (lidline(0.5) + 0.04 + 0.16 * t)) < 0.07 * (1 - t) + 0.02)] = CI
    return c
de = unit(0.42, -0.9, 0.12)
E = H + de * 0.083
def eyes(state):
    k.decal(E, de, (0.036, 0.036), eye_look(state, eye, CB, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
