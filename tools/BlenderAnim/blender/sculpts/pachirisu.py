# Pachirisu sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/pachirisu.py -- <the export's Pachirisu.glb> <out.glb>
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, boxy, eye_look, args

src, out = args()
k = Kit(src, coat="fur")

WHITE, BLUE, YELLOW, INK = "#F6F7FB", "#5EC3E6", "#F7D23E", "#1C1C26"
MOUTH, TONGUE, NOSE = "#8E2734", "#EE7884", "#3A3440"
H = np.array([0, -0.02, 0.42])           # the head's centre
HR = np.array([0.13, 0.112, 0.106])

# A white squirrel: a big round head on a small pear of a body
k.add("head", ell(H, HR, deform=boxy(2.3)), WHITE, "head")
def pear(V):
    V = V.copy(); s = 1 - 0.16 * V[:, 2]
    V[:, 0] *= s; V[:, 1] *= s
    return V
k.add("body", ell((0, 0.0, 0.2), (0.1, 0.086, 0.115), deform=pear), WHITE, "body")
k.add("neck", ell((0, -0.008, 0.31), (0.082, 0.07, 0.06)), WHITE, "body")

# Short legs and little feet
k.add("thigh", ell((0.062, -0.008, 0.098), (0.047, 0.05, 0.066)), WHITE, "leg{s}", mirror=True)
k.add("foot", ell((0.066, -0.042, 0.026), (0.036, 0.056, 0.026)), WHITE, "leg{s}", mirror=True)

# Little arms with their paws held up to the chest
k.add("arm", tube([(0.07, -0.02, 0.27), (0.088, -0.068, 0.252), (0.062, -0.1, 0.268)], [0.028, 0.024, 0.021]),
      WHITE, "arm{s}", mirror=True)
k.add("paw", ell((0.05, -0.108, 0.282), (0.024, 0.022, 0.027)), WHITE, "arm{s}", mirror=True)

# Blue pointed ears, standing up and out
k.add("ear", blade([(0.072, 0.0, 0.46), (0.106, 0.004, 0.55), (0.142, 0.01, 0.635)], [0.05, 0.03, 0.004],
                   (0, 1, 0), 0.4), BLUE, "ear{s}", mirror=True)

# The great tail: up behind the body, over at the head's height and curled down into a spiral behind
TP = np.array([(0, 0.05, 0.15), (0, 0.13, 0.2), (0, 0.2, 0.31), (0, 0.26, 0.42), (0, 0.35, 0.455), (0, 0.43, 0.39),
               (0, 0.43, 0.28), (0, 0.36, 0.24), (0, 0.33, 0.29)])
TR = np.array([0.05, 0.085, 0.1, 0.105, 0.1, 0.09, 0.075, 0.06, 0.045])
k.add("tail", tube(TP, TR, seg=24), WHITE, "tail")
C = np.array([0.31, 0.33])                 # the curl's middle, in (y, z)
# The curl's middle filled, set back from its sides so that the spiral still shows as a groove
k.add("tailcurl", ell((0, 0.34, 0.33), (0.055, 0.05, 0.05)), WHITE, "tail")

# The tail's centre line, as the tube draws it, with each point's radius, the way out from the curl and how far round
def line(per=24):
    ext = np.vstack([2 * TP[0] - TP[1], TP, 2 * TP[-1] - TP[-2]])
    Q, R = [], []
    for i in range(len(TP) - 1):
        p0, p1, p2, p3 = ext[i], ext[i + 1], ext[i + 2], ext[i + 3]
        for j in range(per):
            t = j / per
            Q.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                            + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
            R.append(TR[i] + (TR[i + 1] - TR[i]) * t)
    Q.append(TP[-1]); R.append(TR[-1])
    Q, R = np.array(Q), np.array(R)
    N = np.zeros_like(Q); N[:, 1:] = Q[:, 1:] - C
    N /= np.linalg.norm(N, axis=1, keepdims=True)
    phi = np.degrees(np.arctan2(Q[:, 2] - C[1], Q[:, 1] - C[0]))
    phi = np.where(phi < -90, phi + 360, phi)
    T = np.gradient(Q, axis=0); T /= np.linalg.norm(T, axis=1, keepdims=True)
    return Q, R, N, phi, T
LQ, LR, LN, LPHI, LT = line()

# Three blue spikes along the outside of the curl, leaning back
for i, at in enumerate((0.32, 0.44, 0.56)):
    j = int(at * (len(LQ) - 1))
    q, r, n, t = LQ[j], LR[j], LN[j], LT[j]
    d = n + 0.55 * t; d /= np.linalg.norm(d)
    k.add(f"tailspike{i}", blade([q + n * r * 0.55, q + n * r + d * 0.05, q + n * r + d * 0.095],
                                 [0.042, 0.022, 0.003], (1, 0, 0), 0.45), BLUE, "tail")

# The blue stripe: a point on the brow, wide between the ears, then narrow over the back of the head, down the back
# and along the outside of the tail's curl to where it turns down. Its edges are under a voxel
def head_stripe(P):
    d = P - H
    a = np.degrees(np.arctan2(d[:, 2], -d[:, 1])) % 360
    w = np.where(a < 82, 0.05 * np.clip((a - 30) / 52, 0, 1) ** 0.85,
                 0.05 - 0.024 * smoothstep(82, 150, a))
    w = np.where(a > 262, 0, w)
    near = (np.linalg.norm(d / (HR * 1.25), axis=1) < 1) * (P[:, 2] > 0.3)
    return inside(np.abs(P[:, 0]) - w, 0.0022) * near
k.paint(head_stripe, BLUE)
def back_stripe(P):
    back = (P[:, 2] > 0.22) * (P[:, 2] < 0.34) * smoothstep(0.0, 0.02, P[:, 1])
    return inside(np.abs(P[:, 0]) - 0.026, 0.0022) * back
k.paint(back_stripe, BLUE)
def tail_stripe(P):
    j = np.concatenate([((P[s:s + 4000, None, :] - LQ[None]) ** 2).sum(2).argmin(1) for s in range(0, len(P), 4000)])
    q, r, n = LQ[j], LR[j], LN[j]
    off = P - q
    dist = np.linalg.norm(off, axis=1)
    out = (off * n).sum(1)
    wx = (0.024 + 0.05 * smoothstep(0.15, 0.45, j / (len(LQ) - 1))) * smoothstep(8, 40, LPHI[j]) * smoothstep(0.15, 0.22, j / (len(LQ) - 1))
    return inside(np.abs(P[:, 0]) - wx, 0.0022) * inside(-out, 0.004) * (dist < r + 0.02)
k.paint(tail_stripe, BLUE)

# The face: a tiny nose, an open mouth with one front tooth, round yellow cheeks
k.add("nose", ell((0, -0.1315, 0.414), (0.008, 0.005, 0.0055)), NOSE, "head", fuse=False, surface="skin")
CW, CM, CT, CI = srgb(WHITE), srgb(MOUTH), srgb(TONGUE), srgb(INK)
def mouth(u, v):
    c = np.tile(CM, (len(u), 1))
    c[(u / 0.66) ** 2 + ((v + 0.75) / 0.55) ** 2 < 1] = CT
    c[(np.abs(u) < 0.2) & (v > 0.02)] = CW
    return c
# The mouth's own outline is its shape, flat along the top: a colour's edge inside a patch comes out ragged
def mouth_shape(th):
    s = math.sin(th)
    return min(1.0, 0.36 / s) if s > 0 else 1.0
k.decal((0, -0.13, 0.38), unit(0, -1, -0.38), (0.033, 0.03), mouth, eps=0.0025, outline=mouth_shape, rings=24,
        segs=72)
dc = unit(0.8, -0.48, -0.36)
k.decal(H + dc * 0.11, dc, (0.036, 0.034), YELLOW, eps=0.0025, rings=10, segs=40, mirror=True)

# Big dark eyes with a glint, set well apart
def eye(u, v):
    c = np.tile(CI, (len(u), 1))
    c[((u - 0.12) / 0.34) ** 2 + ((v - 0.42) / 0.3) ** 2 < 1] = (1, 1, 1)
    c[((u + 0.25) / 0.14) ** 2 + ((v + 0.38) / 0.12) ** 2 < 1] = (0.72, 0.74, 0.8)
    return c
de = unit(0.44, -0.89, 0.12)
E = H + de * 0.112
def eyes(state):
    k.decal(E, de, (0.024, 0.033), eye_look(state, eye, CW, CI), eps=0.004, rings=20, segs=56, mirror=True)

k.build(voxel=0.005)
k.eye_states(eyes)
k.export(out)
