# Hippowdon sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/hippowdon.py -- <the export's Hippowdon.glb> <out.glb>
# Hippopotas grown into a great sand hippo (the male's colours): its jaws thrown wide open, the upper one grey with a
# row of dark nostril holes on its snout, the lower one yellow, the mouth red inside with short round white teeth;
# small red eyes peering out of raised grey mounds on the top of the head; a barrel of a yellow body with grey plates
# on the back, each with a dark vent ringed in grey; thick yellow legs with grey cuffs and pale claws; a stub of a tail.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

YELLOW, GREY, MOUTH, TOOTH, HOLE, NAIL = "#D6B24E", "#626066", "#B8404E", "#F4F2EC", "#1A1416", "#EDEAE2"


def oriented(c, axes, r, deform=None):
    """An ellipsoid about c whose radii r lie along the three axes given (each a unit vector)."""
    V, F = ell((0, 0, 0), r, deform=deform)
    return V @ np.stack(axes, axis=0) + np.array(c), F


X = np.array([1.0, 0, 0])
# The jaws' frames: a runs out along the jaw, n away from the mouth (up for the upper jaw, down for the lower)
UA = unit(0, -math.cos(math.radians(52)), math.sin(math.radians(52)))
UN = np.cross(X, UA) * -1                         # (0, +, +): over the snout, away from the mouth
UP0 = np.array([0.0, -0.22, 0.41])                # the head's joint, where the upper jaw hinges
UC = UP0 + UA * 0.17
LA = unit(0, -math.cos(math.radians(10)), -math.sin(math.radians(10)))
LN = np.cross(X, LA)                              # (0, -, +)... turned so that it points up, into the mouth
if LN[2] < 0:
    LN = -LN
LP0 = np.array([0.0, -0.2, 0.36])                 # the jaw's joint
LC = LP0 + LA * 0.165


def local(P, c, a, n):
    d = P - c
    return d @ X, d @ a, d @ n


# --- the body, the head behind the jaws, the tail ------------------------------------------------------------------
k.add("body", ell((0, 0.06, 0.3), (0.26, 0.32, 0.205)), YELLOW, "body")
k.add("neck", ell((0, -0.13, 0.36), (0.21, 0.13, 0.16)), YELLOW, "body")
k.add("head", ell((0, -0.24, 0.43), (0.175, 0.12, 0.13)), YELLOW, "head")
k.add("tail", tube([(0, 0.35, 0.33), (0, 0.4, 0.31), (0, 0.44, 0.29)], [0.04, 0.026, 0.008]), GREY, "tail")

# --- the jaws: a grey upper one tilted up, a yellow lower one, red within, a red throat at the hinge -----------------
k.add("upjaw", oriented(UC, [X, UA, UN], (0.19, 0.18, 0.072), deform=boxy(2.6)), GREY, "head")
k.add("lowjaw", oriented(LC, [X, LA, LN], (0.19, 0.17, 0.065), deform=boxy(2.6)), YELLOW, "jaw")
k.add("throat", ell((0, -0.3, 0.45), (0.13, 0.06, 0.09)), MOUTH, "head")
# The cheeks join the two jaws at the back of the mouth, yellow
k.add("cheek", ell((0.155, -0.3, 0.42), (0.04, 0.09, 0.08)), YELLOW, "head", mirror=True)


def palate(P):
    x, a, n = local(P, UC, UA, UN)
    return inside(n + 0.012, 0.004) * inside(np.abs(x) - 0.158, 0.005) * inside(a - 0.152, 0.005)


def tongue(P):
    x, a, n = local(P, LC, LA, LN)
    return inside(-n + 0.012, 0.004) * inside(np.abs(x) - 0.152, 0.005) * inside(a - 0.138, 0.005)


k.paint(palate, MOUTH, on=["upjaw", "throat", "cheek"])
k.paint(tongue, MOUTH, on=["lowjaw", "throat", "cheek"])
# The back of the mouth, red wherever the head, neck or cheeks show between the jaws
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.145, 0.005) * inside(P[:, 1] + 0.255, 0.006)
        * inside(np.abs(P[:, 2] - 0.44) - 0.085, 0.006), MOUTH, on=["throat", "neck", "head", "cheek"])

# The teeth: short round white stubs, a big pair at the corners and a smaller pair between, top and bottom
for i, (x, a, ln, r) in enumerate([(0.122, 0.118, 0.026, 0.026), (0.05, 0.14, 0.02, 0.02)]):
    for s in (1, -1):
        c = UC + X * x * s + UA * a - UN * (0.06 + ln * 0.6)
        k.add(f"uptooth{i}{'R' if s > 0 else 'L'}", oriented(c, [X, UA, UN], (r, r * 0.9, ln)), TOOTH, "head",
              fuse=False, surface="shell")
for i, (x, a, ln, r) in enumerate([(0.125, 0.105, 0.036, 0.028), (0.05, 0.128, 0.024, 0.021)]):
    for s in (1, -1):
        c = LC + X * x * s + LA * a + LN * (0.05 + ln * 0.6)
        k.add(f"lowtooth{i}{'R' if s > 0 else 'L'}", oriented(c, [X, LA, LN], (r, r * 0.9, ln)), TOOTH, "jaw",
              fuse=False, surface="shell")

# --- the mounds the eyes peer out of, on the top of the head behind the upper jaw -----------------------------------
EM = np.array([0.125, -0.215, 0.535])
k.add("mound", ell(EM, (0.06, 0.06, 0.05)), GREY, "head", mirror=True)

# --- the back: grey plates, each with a dark vent in a grey rim -----------------------------------------------------
k.add("spine", ell((0, 0.08, 0.48), (0.12, 0.25, 0.06)), GREY, "body")
PLATES = [((0.12, -0.02, 0.47), (0.12, 0.12, 0.07)), ((0.13, 0.19, 0.45), (0.12, 0.12, 0.07))]
for i, (c, r) in enumerate(PLATES):
    k.add(f"plate{i}", ell(c, r), GREY, "body", mirror=True)
k.paint(lambda P: inside(0.47 - P[:, 2] + 0.012 * np.sin(P[:, 1] * 30), 0.004) * (P[:, 1] > -0.13), GREY,
        on=["body", "neck"])


def vent(c, n, r, t, name, bone):
    n = unit(*n)
    a = np.cross(n, (0, 0, 1.0)); a /= np.linalg.norm(a); b = np.cross(n, a)
    pts = [np.array(c) + r * (math.cos(q) * a + math.sin(q) * b) for q in np.linspace(0, 2 * math.pi, 25)]
    k.add(name, tube(pts, [t] * len(pts)), GREY, bone, mirror=True)
    k.add(name + "hole", ell(np.array(c) - n * 0.01, (r * 0.85, r * 0.85, r * 0.85)), HOLE, bone, carve=True,
          mirror=True)


vent((0.19, -0.01, 0.5), (0.65, -0.1, 0.75), 0.05, 0.016, "ventf", "body")
vent((0.2, 0.2, 0.48), (0.65, 0.1, 0.75), 0.05, 0.016, "ventb", "body")

# --- the legs: thick and yellow, a grey cuff at the foot, three pale claws ------------------------------------------
for name, y, bone in (("leg", -0.12, "leg{s}"), ("hind", 0.24, "hind{s}")):
    k.add(name, tube([(0.17, y, 0.22), (0.185, y - 0.005, 0.08)], [0.1, 0.092]), YELLOW, bone, mirror=True)
    k.add(name + "cuff", ell((0.185, y - 0.005, 0.055), (0.102, 0.105, 0.052)), GREY, bone, mirror=True)
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        p = [(0.185 + dx, y - 0.07, 0.03), (0.185 + dx * 1.4, y - 0.115, 0.014), (0.185 + dx * 1.7, y - 0.14, 0.007)]
        k.add(f"{name}claw{j}", blade(p, [0.02, 0.014, 0.003], (0, 0, 1), 0.55), NAIL, bone, fuse=False, mirror=True,
              surface="shell")

# --- the nostrils: dark holes in a row over the snout ---------------------------------------------------------------
for j, (x, a) in enumerate([(0.05, 0.125), (0.105, 0.09)]):
    c = UC + X * x + UA * a + UN * 0.07
    k.decal(c, UN, (0.017, 0.017), HOLE, eps=0.004, rings=10, segs=32, mirror=True)

# --- the eyes: small red eyes with a dark slit, under a grey lid, on the mounds' outer fronts ----------------------
CR, CK, CG = srgb("#E0262E"), srgb(HOLE), srgb(GREY)
def eye(u, v):
    c = np.tile(CR, (len(u), 1))
    c[((u + 0.08) / 0.3) ** 2 + ((v + 0.08) / 0.42) ** 2 < 1] = CK
    c[v > 0.45 - 0.25 * u] = CG                       # the lid's line, low toward the snout
    c[np.abs(v - (0.45 - 0.25 * u)) < 0.12] = CK
    return c
ED = unit(0.72, -0.55, 0.42)
def eyes(state):
    k.decal(EM + ED * 0.058, ED, (0.034, 0.032), eye_look(state, eye, CG, CK, line=0.16), eps=0.004, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.008, smooth=3)
k.eye_states(eyes)
k.export(out)
