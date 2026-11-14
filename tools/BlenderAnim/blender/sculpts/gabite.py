# Gabite sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/gabite.py -- <the export's Gabite.glb> <out.glb>
# Gible grown up: the same blue and red, standing upright on bent legs. A long neck holds the head forward; the head
# is a downturned snout under a crown with two round jet-like lobes, a pale band round each, a red lower jaw with two
# white fangs over it, small yellow slit eyes under a heavy brow. The red runs from the jaw down the throat and belly
# to a notched end over a pale lower belly. A big notched dorsal fin, a small fin on each forearm, one white claw a
# hand, two white spikes on each thigh, three white claws a foot, a thick tail with a fin on its top.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from mathutils import Euler
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, BAND, RED, PALE, WHITE = "#5585BA", "#CFCDF1", "#D5513F", "#BDBBE8", "#F4F5F7"
DARK = "#3C5F8C"


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


# The body: a slim chest under the long neck, widening to a full belly and hips
# one smooth trunk (stacked ellipsoids left the red front ribbed), a little flattened front to back
TV0, TF0 = tube([(0, 0.01, 0.36), (0, 0.01, 0.46), (0, 0.015, 0.59), (0, 0.02, 0.72), (0, 0.02, 0.78)],
                [0.165, 0.175, 0.14, 0.12, 0.1])
TV0[:, 1] = (TV0[:, 1] - 0.015) * 0.85 + 0.015
k.add("torso", (TV0, TF0), BLUE, "body")
k.add("belly", ell((0, 0.01, 0.4), (0.19, 0.15, 0.15)), BLUE, "body")
k.add("neck", tube([(0, 0.01, 0.7), (0, -0.0, 0.82), (0, -0.04, 0.92)], [0.085, 0.066, 0.06]), BLUE, "head")

# The head: a crown over a snout that turns down and forward, the lower jaw red under it
HC = np.array([0.0, -0.02, 1.02])
k.add("crown", ell(HC, (0.105, 0.12, 0.085), rot=(-8, 0, 0)), BLUE, "head")
k.add("snout", ell((0, -0.14, 0.975), (0.064, 0.115, 0.062), rot=(22, 0, 0)), BLUE, "head")
k.add("brow", ell((0.0, -0.1, 1.035), (0.085, 0.07, 0.03), rot=(18, 0, 0)), BLUE, "head")
JC, JR, JROT = np.array([0, -0.13, 0.925]), np.array([0.053, 0.088, 0.032]), (16, 0, 0)
k.add("jaw", ell(JC, JR, rot=JROT), RED, "jaw")
# The two lobes on the crown, short and fat, up and out like a hammerhead's, a pale band round each before its end
LA, LB = np.array([0.035, 0.01, 1.04]), np.array([0.15, 0.03, 1.11])
k.add("lobe", tube([LA, (LA + LB) / 2, LB], [0.064, 0.074, 0.076]), BLUE, "head", mirror=True)
LD = unit(*(LB - LA))
def band(P):
    q = P.copy(); q[:, 0] = np.abs(q[:, 0])
    t = (q - LB) @ LD
    return inside(np.abs(t + 0.015) - 0.026, 0.004) * smoothstep(0.08, 0.1, q[:, 0])
k.paint(band, BAND, on=["lobe"])

# The red front: from the jaw down the throat and the belly to two points with a notch between, the pale lower belly
# under it
def ycl(z):
    return np.interp(z, [0.4, 0.7, 0.85, 0.95], [0.0, 0.02, 0.0, -0.06])
def front_angle(P):
    return np.abs(np.arctan2(P[:, 0], -(P[:, 1] - ycl(P[:, 2]))))
def red_front(P):
    a = front_angle(P)
    reach = np.interp(P[:, 2], [0.4, 0.55, 0.75, 0.9], [0.8, 0.9, 0.8, 0.95])
    bottom = 0.36 + 0.05 * np.clip(1 - a / 0.3, 0, 1)
    return inside(a - reach, 0.006) * inside(bottom - P[:, 2], 0.004) * inside(P[:, 2] - 0.905, 0.004)
def pale_front(P):
    a = front_angle(P)
    return inside(a - 1.05, 0.01) * inside(P[:, 2] - 0.45, 0.004) * inside(0.23 - P[:, 2], 0.004)
k.paint(pale_front, PALE, on=["belly", "waist"])
k.paint(red_front, RED, on=["neck", "torso", "belly", "waist"])
# The head blue to a crisp lip line: only the jaw itself is red up there
def off_jaw(P):
    mrot = np.array(Euler([math.radians(a) for a in JROT]).to_matrix())
    q = (P - JC) @ mrot / JR
    e = np.sqrt((q ** 2).sum(1)) - 1
    above = np.maximum(inside(0.925 - P[:, 2], 0.004),
                       inside(0.875 - P[:, 2], 0.004) * inside(-0.07 - P[:, 1], 0.004))
    return inside(0.08 - e, 0.01) * above
k.paint(off_jaw, BLUE, on=["crown", "snout", "brow", "neck", "jaw"])

# The legs: heavy thighs, the knee forward, the shin back down to the heel, a broad foot
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"thigh{side}", ell(np.array((0.165, -0.02, 0.3)) * m, (0.115, 0.135, 0.145), rot=(-15, 0, 0)), BLUE, f"leg{side}")
    k.add(f"shin{side}", tube(np.array([(0.17, -0.05, 0.2), (0.17, 0.02, 0.12), (0.17, 0.04, 0.07)]) * m,
                              [0.075, 0.06, 0.05]), BLUE, f"leg{side}")
    k.add(f"foot{side}", ell(np.array((0.17, -0.045, 0.045)) * m, (0.078, 0.118, 0.048)), BLUE, f"leg{side}")
    for j, dx in enumerate((-0.04, 0.0, 0.04)):
        b = np.array((0.17 + dx, -0.14, 0.032))
        k.add(f"toe{side}{j}", blade(np.array([b, b + (dx * 0.3, -0.028, -0.006), b + (dx * 0.45, -0.05, -0.016)]) * m,
                                     [0.018, 0.011, 0.003], (0, 0, 1), 0.55), WHITE, f"leg{side}", fuse=False,
              surface="shell")
    # Two white spikes off the outer thigh, pointing out and down
    for j, (b, d, l) in enumerate([((0.265, -0.05, 0.36), (1, -0.35, -0.45), 0.09),
                                   ((0.26, -0.03, 0.26), (1, -0.25, -0.6), 0.065)]):
        b = np.array(b); d = unit(*d)
        k.add(f"spike{side}{j}", blade(np.array([b - d * 0.02, b + d * l * 0.5, b + d * l]) * m,
                                       [0.022, 0.014, 0.002], (0, 1, 0), 0.5), WHITE, f"leg{side}", fuse=False,
              surface="shell")

# The arms: thin, out and down from the shoulders, the forearm forward, one white claw a hand, a small fin on the
# back of each forearm
for s, side in ((1, "R"), (-1, "L")):
    m = np.array([s, 1, 1.0])
    k.add(f"arm{side}", tube(np.array([(0.15, 0.0, 0.79), (0.24, 0.0, 0.73), (0.31, -0.04, 0.66), (0.35, -0.1, 0.58)]) * m,
                             [0.048, 0.04, 0.036, 0.032]), BLUE, f"arm{side}")
    Hn = np.array([0.355, -0.115, 0.565])
    d = unit(0.25, -0.45, -1)
    k.add(f"claw{side}", blade(np.array([Hn, Hn + d * 0.045, Hn + d * 0.09]) * m, [0.024, 0.015, 0.002],
                               (0, 1, 0), 0.5), WHITE, f"arm{side}", fuse=False, surface="shell")
    # The fin: a triangle off the back of the forearm, its point back and a little down
    E, M = np.array((0.245, 0.005, 0.728)), np.array((0.33, -0.07, 0.62))
    ex = unit(*(M - E)); ez = np.array((0, 1.0, -0.1)); ez = ez - ex * (ez @ ex); ez /= np.linalg.norm(ez)
    FIN = [(-0.01, 0.0), (0.13, 0.0), (0.0, 0.12)]
    FV, FF = flat(FIN, E * m, ex * m, ez * m, 0.024)
    k.add(f"armfin{side}", (FV, FF), BLUE, f"arm{side}", fuse=False, surface="scales")

# The tail: thick at the hips, out back and a little down to a point, a fin standing on its top
k.add("tail", tube([(0, 0.1, 0.45), (0, 0.27, 0.36), (0, 0.45, 0.27), (0, 0.6, 0.22)],
                   [0.11, 0.08, 0.05, 0.012]), BLUE, "tail")
TF = [(0.28, 0.3), (0.42, 0.42), (0.44, 0.28)]
TV, TFF = flat(TF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.03)
k.add("tailfin", (TV, TFF), BLUE, "tail", fuse=False, surface="scales")
# two dark lines across the tail's top
k.paint(lambda P: sum(inside(np.abs(P[:, 1] - y) - 0.009, 0.003) for y in (0.36, 0.43))
        * inside(np.abs(P[:, 0]) - 0.05, 0.004), DARK, on=["tail"])

# The dorsal fin: a big plate swept up and back off the shoulders, a step notched in its trailing edge
DF = [(0.04, 0.84), (0.2, 0.96), (0.42, 1.07), (0.37, 0.93), (0.4, 0.9), (0.3, 0.76), (0.17, 0.6), (0.08, 0.66)]
DV, DFF = flat(DF, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.05)
DV[:, 0] *= np.clip(1 - 0.75 * (DV[:, 1] - 0.12) / 0.3, 0.3, 1)
k.add("dorsal", (DV, DFF), BLUE, "body", fuse=False, surface="scales")
k.add("dorsalroot", blade([(0, 0.09, 0.82), (0, 0.13, 0.72), (0, 0.15, 0.63)], [0.05, 0.06, 0.05], (1, 0, 0), 0.6),
      BLUE, "body")

# Two white fangs from the upper lip over the red jaw, near the front
for x in (0.033,):
    p = np.array([x, -0.2, 0.93])
    k.add("fang", blade([p, p + (0.0, 0.0, -0.02), p + (0.0, 0.002, -0.04)], [0.013, 0.009, 0.002], (0, 1, 0), 0.5),
          WHITE, "head", fuse=False, mirror=True, surface="shell")

# The eyes: small and fierce, a black rim, a yellow iris with a black slit, the top cut by the brow sloping down
# toward the snout. On a sideways patch +u runs toward the back; the left eye's patch is laid with its picture and
# outline turned over.
INK, YELLOW, LID = srgb("#141820"), srgb("#F2C21E"), srgb(BLUE)
BA, BB = 0.3, 0.42
def cut(th):
    s = math.sin(th) - BB * math.cos(th)
    return min(1.0, BA / s) if s > 1e-6 else 1.0
def eye(u, v):
    reach = np.array([cut(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(INK, (len(u), 1))
    c[rho < 0.78] = YELLOW
    c[((u + 0.05) / 0.16) ** 2 + ((v + 0.1) / 0.62) ** 2 < 1] = INK
    return c
de = unit(0.6, -0.75, 0.18)
EC = np.array([0.058, -0.155, 1.0])
def eyes(state):
    look = eye_look(state, eye, LID, INK, line=0.16)
    k.decal(EC, de, (0.032, 0.036), look, eps=0.006, rings=20, segs=56, outline=cut)
    m = np.array([-1, 1, 1.0])
    k.decal(EC * m, de * m, (0.032, 0.036), lambda u, v: look(-u, v), eps=0.006, rings=20, segs=56,
            outline=lambda th: cut(math.pi - th))

k.build(voxel=0.008, smooth=3)
k.eye_states(eyes)
k.export(out)
