# Garchomp sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/garchomp.py -- <the export's Garchomp.glb> <out.glb>
# Gabite grown into a sleek land shark of deep navy: a hammerhead's head, its two horns lying out to the sides with a
# dark ring near each round end, a yellow star on the brow, the jaws open on a few big white teeth, small yellow slit
# eyes under the brow. The red runs from the lower jaw down the throat and spreads over the chest, ending at the
# waist on a yellow band with a point down its middle. Arms out and a little down with great scythe fins hanging under
# the forearms and one white claw at each wrist, white spikes on the upper arms and the thighs, three white claws a
# foot, ringed shins, a big dorsal fin with a notch (the male's), a long tail ending in a shark's two-lobed fin.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, polygon, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

NAVY, DARK, RED, YELLOW, WHITE, MOUTH = "#4F5594", "#363B6E", "#D8483B", "#F5C22E", "#F4F5F7", "#8C2630"


def flat(outline, origin, ex, ez, thick):
    """A flat plate of a polygon (a, b in the plane of ex, ez about origin), thick about its middle, sharp edged."""
    ex, ez = unit(*ex), unit(*ez)
    nrm = np.cross(ex, ez); nrm /= np.linalg.norm(nrm)
    p = np.array(outline, float)
    if 0.5 * np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]) < 0:
        p = p[::-1]
    W = np.array(origin, float) + p[:, :1] * ex + p[:, 1:] * ez
    m = len(p)
    front, back = W + nrm * thick / 2, W - nrm * thick / 2
    idx, tris = list(range(m)), []
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    while len(idx) > 3:
        for i in range(len(idx)):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % len(idx)]
            if cross(p[a], p[b], p[c]) <= 1e-12:
                continue
            if any(cross(p[a], p[b], p[j]) >= 0 and cross(p[b], p[c], p[j]) >= 0 and cross(p[c], p[a], p[j]) >= 0
                   for j in idx if j not in (a, b, c)):
                continue
            tris.append((a, b, c)); idx.pop(i)
            break
        else:
            break
    tris.append(tuple(idx))
    F = []
    for a, b, c in tris:
        F.append([a, b, c]); F.append([m + c, m + b, m + a])
    rim = []
    for i in range(m):
        j = (i + 1) % m
        rim.extend([front[i], back[i], back[j], front[j]])
        F.append([2 * m + 4 * i, 2 * m + 4 * i + 1, 2 * m + 4 * i + 2, 2 * m + 4 * i + 3])
    return np.vstack([front, back, np.array(rim)]), F


def spike(name, b, d, l, w, bone):
    b = np.array(b, float); d = unit(*d)
    return blade(np.array([b - d * 0.02, b + d * l * 0.5, b + d * l]), [w, w * 0.62, 0.002], (0, 1, 0), 0.5)


# The body: a broad chest narrowing to a slim waist over wide hips, one smooth trunk a little flat front to back
TV0, TF0 = tube([(0, 0.01, 0.36), (0, 0.01, 0.46), (0, 0.015, 0.58), (0, 0.02, 0.71), (0, 0.02, 0.8)],
                [0.16, 0.155, 0.125, 0.145, 0.11])
TV0[:, 1] = (TV0[:, 1] - 0.015) * 0.82 + 0.015
k.add("torso", (TV0, TF0), NAVY, "body")
k.add("hips", ell((0, 0.01, 0.4), (0.18, 0.14, 0.13)), NAVY, "body")
k.add("neck", tube([(0, 0.015, 0.76), (0, 0.0, 0.86), (0, -0.035, 0.93)], [0.085, 0.066, 0.06]), NAVY, "head")

# The head: a crown over a long snout held level; the lower jaw dropped open under it, the mouth's dark red inside
# between them
k.add("crown", ell((0, -0.02, 1.03), (0.1, 0.12, 0.075), rot=(-5, 0, 0)), NAVY, "head")
k.add("snout", ell((0, -0.15, 1.005), (0.062, 0.13, 0.048), rot=(-4, 0, 0)), NAVY, "head")
k.add("brow", ell((0.0, -0.1, 1.045), (0.08, 0.07, 0.028), rot=(8, 0, 0)), NAVY, "head")
k.add("inside", ell((0, -0.15, 0.952), (0.044, 0.07, 0.03), rot=(14, 0, 0)), MOUTH, "jaw")
JC, JR, JROT = np.array([0, -0.125, 0.9]), np.array([0.052, 0.11, 0.028]), (28, 0, 0)
k.add("jaw", ell(JC, JR, rot=JROT), RED, "jaw")
# The horns: lying out to the sides and a little back, their round ends drooping, a dark ring near each end
HA, HM, HB = np.array([0.04, 0.0, 1.05]), np.array([0.15, 0.03, 1.08]), np.array([0.235, 0.07, 1.045])
k.add("horn", tube([HA, HM, HB], [0.055, 0.058, 0.052]), NAVY, "head", mirror=True)
HD = unit(*(HB - HM))
k.paint(lambda P: inside(np.abs((np.c_[np.abs(P[:, 0]), P[:, 1:]] - HB) @ HD + 0.035) - 0.009, 0.003)
        * smoothstep(0.16, 0.18, np.abs(P[:, 0])), DARK, on=["horn"])

# The red front: the throat from the jaw down, spreading over the chest to points under the shoulders, narrowing to
# the waist; under it a yellow band whose bottom comes to a point in the middle; navy below
def ycl(z):
    return np.interp(z, [0.4, 0.7, 0.85, 0.95], [0.0, 0.02, 0.0, -0.06])
def front_angle(P):
    return np.abs(np.arctan2(P[:, 0], -(P[:, 1] - ycl(P[:, 2]))))
def red_front(P):
    a = front_angle(P)
    reach = np.interp(P[:, 2], [0.5, 0.6, 0.7, 0.76, 0.84, 0.9], [0.8, 0.85, 1.25, 1.45, 0.85, 0.85])
    return inside(a - reach, 0.006) * inside(0.53 - P[:, 2], 0.003) * inside(P[:, 2] - 0.9, 0.004)
def yellow_band(P):
    a = front_angle(P)
    bottom = 0.47 - 0.07 * np.clip(1 - a / 0.7, 0, 1)
    return inside(a - 0.95, 0.006) * inside(bottom - P[:, 2], 0.003) * inside(P[:, 2] - 0.53, 0.003)
k.paint(red_front, RED, on=["neck", "torso", "hips"])
k.paint(yellow_band, YELLOW, on=["torso", "hips"])
# The head navy to a crisp lip line: only the jaw is red up there
def off_jaw(P):
    mrot = np.array(Euler([math.radians(a) for a in JROT]).to_matrix())
    q = (P - JC) @ mrot / JR
    e = np.sqrt((q ** 2).sum(1)) - 1
    above = np.maximum(inside(0.92 - P[:, 2], 0.004),
                       inside(0.87 - P[:, 2], 0.004) * inside(-0.06 - P[:, 1], 0.004))
    return inside(0.08 - e, 0.01) * above
k.paint(off_jaw, NAVY, on=["crown", "snout", "brow", "neck"])

# The legs: heavy thighs, the knee forward, the shin back down to the heel with two dark rings, a broad foot
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.17, -0.02, 0.32)) * m, (0.11, 0.13, 0.15), rot=(-15, 0, 0)), NAVY, f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.18, -0.05, 0.21), (0.18, 0.02, 0.13), (0.18, 0.04, 0.07)]) * m,
                              [0.07, 0.056, 0.048]), NAVY, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.18, -0.045, 0.045)) * m, (0.075, 0.118, 0.046)), NAVY, f"leg{side}")
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.18 + dx, -0.14, 0.032))
        k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.03, -0.006), b + (dx * 0.45, -0.055, -0.016)]) * m,
                                     [0.019, 0.012, 0.003], (0, 0, 1), 0.55), WHITE, f"leg{side}", fuse=False,
              surface="shell")
    for j, (b, d, l) in enumerate([((0.27, -0.05, 0.38), (1, -0.35, -0.4), 0.095),
                                   ((0.27, -0.03, 0.28), (1, -0.25, -0.6), 0.07)]):
        k.add(f"spike{side}{j}", (spike("", np.array(b) * m, np.array(d) * m, l, 0.023, None)), WHITE, f"leg{side}",
              fuse=False, surface="shell")
k.paint(lambda P: sum(inside(np.abs(P[:, 2] - z) - 0.007, 0.003) for z in (0.1, 0.135))
        * inside(np.abs(np.abs(P[:, 0]) - 0.18) - 0.09, 0.004), DARK, on=["shin"])

# The arms: out from the shoulders and a little down and forward; a scythe fin hanging under each forearm, sweeping
# back as it goes down; a white claw hooked up and out at each wrist; two white spikes on the upper arm
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.17, 0.0, 0.8), (0.27, -0.01, 0.77), (0.35, -0.04, 0.73), (0.42, -0.08, 0.69)]) * m,
                             [0.055, 0.046, 0.04, 0.034]), NAVY, f"arm{side}")
    W = np.array([0.43, -0.085, 0.685])
    d = unit(0.55, -0.25, 0.8)
    k.add(f"claw{side}", blade(np.array([W - d * 0.01, W + d * 0.05, W + d * 0.1 + np.array((0.02, 0, 0))]) * m,
                               [0.026, 0.016, 0.002], (0, 1, 0), 0.5), WHITE, f"arm{side}", fuse=False, surface="shell")
    FIN = [(0.27, 0.78), (0.44, 0.69), (0.44, 0.6), (0.41, 0.38), (0.345, 0.55), (0.28, 0.7)]
    FV, FF = flat([(x * s, z) for x, z in FIN], (0, -0.02, 0), (1, 0, 0), (0, 0, 1), 0.026)
    FV[:, 1] += -0.06 * (FV[:, 0] * s - 0.27) / 0.17 + 0.22 * np.clip(0.7 - FV[:, 2], 0, 1)
    k.add(f"armfin{side}", (FV, FF), NAVY, f"arm{side}", fuse=False, surface="scales")
    for j, (b, l) in enumerate([((0.25, 0.0, 0.81), 0.06), ((0.31, -0.02, 0.79), 0.05)]):
        k.add(f"armspike{side}{j}", spike("", np.array(b) * m, np.array((0.15 * s, 0.25, 1)), l, 0.02, None), WHITE,
              f"arm{side}", fuse=False, surface="shell")

# The tail: thick at the hips, long, out back and down near the ground, ending in a shark's fin of two lobes
k.add("tail", tube([(0, 0.1, 0.45), (0, 0.3, 0.33), (0, 0.52, 0.21), (0, 0.72, 0.15)],
                   [0.11, 0.085, 0.055, 0.025]), NAVY, "tail")
CF = [(0.66, 0.16), (0.8, 0.22), (0.92, 0.38), (0.86, 0.19), (0.9, 0.07), (0.72, 0.12)]
CV, CFF = flat(CF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.03)
k.add("tailfin", (CV, CFF), NAVY, "tail", fuse=False, surface="scales")
k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.008, 0.003) for y in (0.4, 0.47))
        * inside(np.abs(P[:, 0]) - 0.06, 0.004), DARK, on=["tail"])

# The dorsal fin: a big sickle swept up and back off the shoulders, the male's notch in its trailing edge
DF = [(0.04, 0.86), (0.2, 0.98), (0.48, 1.06), (0.4, 0.94), (0.44, 0.9), (0.32, 0.76), (0.18, 0.6), (0.08, 0.68)]
DV, DFF = flat(DF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.05)
DV[:, 0] *= np.clip(1 - 0.75 * (DV[:, 1] - 0.12) / 0.34, 0.3, 1)
k.add("dorsal", (DV, DFF), NAVY, "body", fuse=False, surface="scales")
k.add("dorsalroot", blade([(0, 0.09, 0.84), (0, 0.13, 0.73), (0, 0.15, 0.63)], [0.05, 0.06, 0.05], (1, 0, 0), 0.6),
      NAVY, "body")

# The teeth: two big ones down from the upper jaw and one up from the lower a side, white
for j, (p, l) in enumerate([((0.035, -0.235, 0.975), 0.04), ((0.048, -0.18, 0.975), 0.032)]):
    p = np.array(p)
    k.add(f"tooth{j}", blade([p, p + (0, 0, -l * 0.5), p + (0, 0.002, -l)], [0.012, 0.008, 0.002], (0, 1, 0), 0.5),
          WHITE, "head", fuse=False, mirror=True, surface="shell")
p = np.array((0.035, -0.2, 0.905))
k.add("fang", blade([p, p + (0, 0, 0.016), p + (0, 0.002, 0.032)], [0.011, 0.007, 0.002], (0, 1, 0), 0.5),
      WHITE, "jaw", fuse=False, mirror=True, surface="shell")

# The yellow star on the brow, a patch on the fused surface
STAR = [(math.cos(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45),
         math.sin(math.pi / 2 + i * math.pi / 5) * (1 if i % 2 == 0 else 0.45)) for i in range(10)]
k.decal((0, -0.12, 1.06), unit(0, -0.45, 1), (0.04, 0.04), YELLOW, up=(0, -1, 0), eps=0.005, outline=polygon(STAR),
        rings=10, segs=60)

# The eyes: small and fierce, a black rim, a yellow iris with a black slit, the top cut by the brow sloping down
# toward the snout. On a sideways patch +u runs toward the back; the left eye's patch is laid turned over.
INK, EYE, LID = srgb("#141820"), srgb("#F2C21E"), srgb(NAVY)
BA, BB = 0.25, 0.45
def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0
def eye(u, v):
    reach = np.array([cut(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(INK, (len(u), 1))
    c[rho < 0.76] = EYE
    c[((u + 0.05) / 0.16) ** 2 + ((v + 0.12) / 0.6) ** 2 < 1] = INK
    return c
de = unit(0.62, -0.74, 0.2)
EC = np.array([0.058, -0.14, 1.025])
def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.16)
    k.decal(EC, de, (0.03, 0.033), look, eps=0.006, rings=20, segs=56, outline=cut)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, (0.03, 0.033), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))

k.build(voxel=0.008, smooth=3)
k.eye_states(eyes)
k.export(out)
