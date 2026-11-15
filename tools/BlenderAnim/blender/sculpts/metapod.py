# Metapod sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/metapod.py -- <the export's Metapod.glb> <out.glb>
# A hard green chrysalis curled like a crescent: a pointed top leaning a little back, a face with a jutting chin and
# a notch under it, a round bulk below, and a ringed tail that curls forward along the ground to a point. Darker
# seams cut the back into plates and ring the tail; two half-shut eyes look out from the front of the face.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, blade, args

src, out = args()
k = Kit(src, coat="shell")

GREEN, SEAM = "#84C455", "#4E8A34"
WIDE = 0.94          # the shell is a little narrower from side to side than it is deep


def squeeze(shape, c=0.0):
    V, F = shape
    V = V.copy(); V[:, 0] = c + (V[:, 0] - c) * WIDE
    return V, F


# The upper shell: from the point down to the face's widest, its front sloping forward as it comes down
UP = [(0, 0.035, 0.625), (0, 0.03, 0.56), (0, 0.016, 0.47), (0, 0.0, 0.38), (0, -0.005, 0.3), (0, 0.0, 0.23)]
UPR = [0.006, 0.045, 0.092, 0.128, 0.145, 0.148]
k.add("upper", squeeze(tube(UP, UPR, seg=40, per=10)), GREEN, "body", surface="shell")
# The lower bulk and the tail curling forward along the ground to its point
LO = [(0, 0.0, 0.23), (0, 0.005, 0.16), (0, -0.04, 0.09), (0, -0.12, 0.06), (0, -0.21, 0.045), (0, -0.29, 0.038),
      (0, -0.35, 0.04)]
LOR = [0.148, 0.135, 0.095, 0.065, 0.044, 0.024, 0.005]
k.add("lower", squeeze(tube(LO, LOR, seg=40, per=10)), GREEN, "body", surface="shell")
# The chin: the face juts forward over a notch, flat underneath
def flat_below(V):
    V = V.copy(); V[:, 2] = np.maximum(V[:, 2], -0.35); return V
def pointed(V):
    V = flat_below(V)
    V[:, 0] *= 1 - 0.75 * np.clip(-V[:, 1], 0, 1)          # narrowing to a point at the front
    return V
# A keel down the middle of the face, from near the point to the chin, so the chin is the end of a ridge
k.add("keel", blade([(0, -0.045, 0.53), (0, -0.105, 0.43), (0, -0.14, 0.35)], [0.012, 0.024, 0.03], (1, 0, 0), 0.55),
      GREEN, "body", surface="shell")
k.add("chin",squeeze(ell((0, -0.09, 0.325), (0.075, 0.08, 0.068), rot=(-12, 0, 0), deform=pointed)), GREEN, "body",
      surface="shell")

# The seams: plates on the back and the sides, rings round the tail. A plate's edges go by the angle round the
# shell's upright axis (nought at the front) and the height
def phi_of(P):
    return np.degrees(np.arctan2(P[:, 0] / WIDE, -P[:, 1]))
W = 0.0035
def plates(P):
    a = np.abs(phi_of(P)); z = P[:, 2]
    r = np.sqrt((P[:, 0] / WIDE) ** 2 + P[:, 1] ** 2)
    back = smoothstep(62, 72, a)                   # the face and the front stay smooth
    m = np.zeros(len(P))
    # two seams across the back, bowed upward toward the middle
    for z0 in (0.2, 0.36):
        zc = z0 + 0.03 * smoothstep(90, 180, a)
        m = np.maximum(m, inside(np.abs(z - zc) - W, 0.002) * back)
    # seams up the back between them, staggered from row to row, and one down the middle of the top
    for a0, z1, z2 in ((105, 0.07, 0.2), (150, 0.07, 0.2), (128, 0.2, 0.36), (180, 0.2, 0.36), (105, 0.36, 0.5),
                       (160, 0.36, 0.52)):
        zc_lo = z1 + 0.03 * smoothstep(90, 180, a); zc_hi = z2 + 0.03 * smoothstep(90, 180, a)
        m = np.maximum(m, inside(np.abs(a - a0) * np.pi / 180 * r - W, 0.002) * inside(zc_lo - z, 0.002) *
                       inside(z - zc_hi, 0.002))
    return m * smoothstep(0.03, 0.05, r)
k.paint(plates, SEAM)
# Rings round the tail, square to its line
TL = np.array(LO[2:], float)
TS = np.vstack([TL[i] + (TL[i + 1] - TL[i]) * t for i in range(len(TL) - 1) for t in np.linspace(0, 1, 30, endpoint=False)])
TT = np.gradient(TS, axis=0); TT /= np.linalg.norm(TT, axis=1, keepdims=True)
ARC = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(TS, axis=0), axis=1))])
def rings(P):
    i = np.argmin(((P[:, None, :] - TS[None]) ** 2).sum(2), axis=1)
    s = ARC[i]
    m = np.zeros(len(P))
    for cut in (0.06, 0.115, 0.165, 0.21, 0.25):
        m = np.maximum(m, inside(np.abs(s - cut) - W, 0.002))
    return m * (P[:, 1] < -0.02) * inside(P[:, 2] - 0.11, 0.004)
k.paint(rings, SEAM)

# The eyes on the front of the face, a little to each side: a white eye half covered by a heavy flat lid, the
# black pupil low and toward the front
WHITE, PUPIL, LID, INK, GLINT = srgb("#F2F6F2"), srgb("#161616"), srgb(GREEN), srgb("#22301A"), srgb("#FFFFFF")
# The lid's line is the patch's own top edge (an outline cut by a gently arched line), so it stays crisp
def lid_at(u):
    return 0.16 + 0.05 * (1 - u * u)
_us = np.linspace(-0.96, 0.96, 13)
_th = np.linspace(math.radians(-166), math.radians(-14), 25)
OUTLINE = polygon([(u, lid_at(u)) for u in _us[::-1]] + [(math.cos(t), math.sin(t)) for t in _th])
def eye(u, v):
    c = np.tile(WHITE, (len(u), 1))
    c[((u + 0.28) / 0.28) ** 2 + ((v + 0.02) / 0.3) ** 2 < 1] = PUPIL
    c[v > lid_at(u) - 0.13] = INK
    c[u * u + v * v > 0.8] = INK
    return c
EC = np.array([0.0, 0.0, 0.41])
de = unit(0.74, -0.66, 0.1)
EP = EC + np.array([de[0] * 0.124 * WIDE, de[1] * 0.124, 0.0])
def eyes(state):
    k.decal(EP, de, (0.046, 0.046), eye_look(state, eye, LID, INK), eps=0.005, outline=OUTLINE, rings=20, segs=56,
            mirror=True)

k.build(voxel=0.006, smooth=3)
k.eye_states(eyes)
k.export(out)
