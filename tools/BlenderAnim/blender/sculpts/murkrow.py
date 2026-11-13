# Murkrow sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/murkrow.py -- <the export's Murkrow.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, tri, unit, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

NAVY, DEEP, BEAK, RED, CLAW = "#24396C", "#1B2B55", "#F0CB5A", "#D8302E", "#F4F2EE"

# A round ball of a body, a short neck and a round head
H = np.array([0, -0.075, 0.48])
k.add("body", ell((0, 0.01, 0.29), (0.125, 0.14, 0.125)), NAVY, "body")
k.add("neck", ell((0, -0.045, 0.39), (0.065, 0.06, 0.06)), NAVY, "head")
k.add("head", ell(H, (0.095, 0.095, 0.09)), NAVY, "head")
# Wings folded to the sides, their tips sweeping back
k.add("wing", ell((0.11, 0.035, 0.3), (0.035, 0.11, 0.085), rot=(-20, 0, -6)), NAVY, "wing{s}", mirror=True)

# The ruff: a ring of short feathers splayed out and down where the head meets the body
for i in range(12):
    a = 2 * math.pi * i / 12
    d = np.array([math.sin(a), -math.cos(a), 0])
    base = np.array([0, -0.035, 0.405]) + d * 0.045
    tip = base + d * 0.06 + np.array([0, 0, -0.035 + 0.01 * (i % 2)])
    side = np.cross(d, (0, 0, 1.0))
    k.add(f"ruff{i}", blade([base, (base + tip) / 2, tip], [0.02, 0.016, 0.002], np.cross(side, tip - base), 0.35),
          NAVY, "head", fuse=False)

# The witch's hat: a wide brim tilted down to the back, and a crown of three swept feathers
def brim(V):
    V = V.copy()
    th = np.arctan2(V[:, 1], V[:, 0])
    V[:, 0] *= 1 + 0.04 * np.sin(5 * th); V[:, 1] *= 1 + 0.04 * np.sin(5 * th)
    return V
k.add("brim", ell((0, -0.06, 0.555), (0.175, 0.16, 0.014), rot=(8, 0, 0), deform=brim), NAVY, "head", fuse=False)
k.add("cap", ell((0, -0.055, 0.565), (0.085, 0.085, 0.05)), NAVY, "head", fuse=False)
for i, (b, t, r) in enumerate([((0, -0.07, 0.58), (0.0, -0.06, 0.75), 0.07),
                               ((0.03, -0.03, 0.58), (0.06, 0.06, 0.70), 0.055),
                               ((-0.03, -0.03, 0.58), (-0.06, 0.06, 0.70), 0.055),
                               ((0, -0.1, 0.575), (0.0, -0.17, 0.67), 0.045)]):
    b, t = np.array(b), np.array(t)
    m = b + (t - b) * 0.45 + np.array([0, 0.012, 0])
    k.add(f"crown{i}", blade([b, m, t], [r, r * 0.65, 0.003], (1, 0, 0) if i in (0, 3) else (0, 1, 0), 0.4),
          NAVY, "head", fuse=False)

# The crooked beak: a long upper bill hooked down over a short lower one
k.add("beak", tube([(0, -0.14, 0.475), (0, -0.2, 0.47), (0, -0.245, 0.45), (0.005, -0.258, 0.42), (0.008, -0.248, 0.4)],
                   [0.048, 0.04, 0.028, 0.014, 0.003]), BEAK, "head", fuse=False, surface="shell")
k.add("jaw", ell((0, -0.19, 0.438), (0.035, 0.05, 0.016), rot=(10, 0, 0)), BEAK, "head", fuse=False, surface="shell")

# The broom of a tail: a fan of ragged feathers out behind, a red band where it meets the body
k.add("band", ell((0, 0.155, 0.28), (0.04, 0.025, 0.042)), RED, "tail", fuse=False)
k.add("tailbase", ell((0, 0.27, 0.31), (0.035, 0.13, 0.1), rot=(-10, 0, 0)), NAVY, "tail", fuse=False)
for i, a in enumerate(np.linspace(-38, 52, 9)):
    r = math.radians(a)
    for x in (-0.018, 0.018) if i % 2 else (0.0,):
        d = np.array([x * 2, math.cos(r), math.sin(r)])
        base = np.array([x, 0.17, 0.285])
        L = 0.2 + 0.06 * math.cos(math.radians(a - 8) * 1.6)
        tip = base + d * L
        mid = base + d * L * 0.5
        k.add(f"tail{i}{x}", blade([base, mid, tip], [0.035, 0.03, 0.003], (1, 0, 0), 0.4), NAVY if x == 0 else DEEP,
              "tail", fuse=False)

# Thin yellow legs on yellow feet, three toes forward and one back, white claws
for s, side in ((1, "R"), (-1, "L")):
    x = 0.05 * s
    k.add(f"leg{side}", tube([(x, 0.0, 0.2), (x * 1.08, -0.005, 0.1), (x * 1.1, -0.01, 0.035)], [0.017, 0.014, 0.014]),
          BEAK, f"leg{side}", fuse=False, surface="shell")
    base = np.array([x * 1.1, -0.01, 0.025])
    for i, ang in enumerate((-30, 0, 30, 180)):
        rr = math.radians(ang)
        d = np.array([math.sin(rr), -math.cos(rr), 0])
        tip = base + d * (0.065 if ang != 180 else 0.04) + np.array([0, 0, -0.012])
        k.add(f"toe{side}{i}", tube([base, (base + tip) / 2 + np.array([0, 0, 0.005]), tip], [0.013, 0.011, 0.009]),
              BEAK, f"leg{side}", fuse=False, surface="shell")
        k.add(f"claw{side}{i}", tube([tip, tip + (d + np.array([0, 0, -0.4])) * 0.02], [0.008, 0.0015]), CLAW,
              f"leg{side}", fuse=False, surface="shell")

# The eyes: white with a red iris round a white pupil, the lid half down and slanting to the beak in a scowl
CW, CR, CN, CK = srgb("#F8F6F2"), srgb(RED), srgb(NAVY), srgb("#0E1426")
def eye(u, v):
    c = np.tile(CW, (len(u), 1))
    r = np.sqrt((u + 0.08) ** 2 + (v + 0.12) ** 2)
    c[r < 0.5] = CR
    c[r < 0.3] = CW
    c[r < 0.13] = CR
    lid = 0.3 + 0.3 * u                         # lower toward the beak (u inward is negative)
    c[v > lid] = CN
    c[np.abs(v - lid) < 0.1] = CK
    return c
E = np.array([0.052, -0.148, 0.5])
de = unit(0.55, -0.83, 0.05)
def eyes(state):
    k.decal(E, de, (0.03, 0.036), eye_look(state, eye, CN, CK), eps=0.006, rings=20, segs=56, mirror=True)

k.build(voxel=0.006)
k.eye_states(eyes)
k.export(out)
