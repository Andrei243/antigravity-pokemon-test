# Tentacruel sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/tentacruel.py -- <the export's Tentacruel.glb> <out.glb>
#
# From the references: Tentacool grown. A light blue bell whose upper sides are two great red crystals with a blue
# ridge running over the top between them and a small red crystal at its front; under the crystals the bell flares
# into a wide brim of four points (left, right, front over the face, back). Under the brim a black face with two
# fierce white eyes, two long blue beaks hanging from it, and a mass of grey tentacles, each with a knob at its end.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, sheet, polygon, srgb, inside, smoothstep, unit, eye_look, args

src, out = args()
k = Kit(src, coat="scales")

BLUE, RED, REDHI, DARK, GREY, INK, UNDER = ("#43B2EA", "#DE2A40", "#F58A98", "#14161C", "#958A82", "#0E1014",
                                            "#2C6E9C")
H = np.array([0.0, 0.0, 0.735])           # the bell's centre
HR = np.array([0.215, 0.2, 0.16])

# --- the bell, its ridge, and the black face under it --------------------------------------------------------------
def lowered(V):
    V = V.copy()
    V[:, 2] = np.maximum(V[:, 2], -0.62)
    return V
k.add("bell", ell(H, HR, deform=lowered), BLUE, "body")
# The ridge over the top, from the small crystal at the front to the back, one mass with the bell
ridge = [H + unit(0, -math.cos(a), math.sin(a)) * (HR[1] * 0.8) for a in np.radians([28, 55, 85, 115, 145])]
k.add("ridge", tube(ridge, [0.04, 0.045, 0.046, 0.044, 0.038]), BLUE, "body")
FC = np.array([0.0, -0.05, 0.545])        # the face's middle
FR = np.array([0.13, 0.115, 0.1])
k.add("face", ell(FC, FR), DARK, "body")
# A thin dark seam from the small crystal down the front of the bell to the brim's front point
k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.0035, 0.002) * (P[:, 1] < -0.1) * (P[:, 2] > 0.6) * (P[:, 2] < 0.8),
        UNDER, on=["bell"])

# --- the brim: a flared sheet of four points round the bell's lower edge, dark blue underneath ----------------------
def brim(lower):
    def at(u, v):
        th = 2 * math.pi * u                              # 0 at the back, the seam hidden behind
        v = 0.015 + 0.97 * v
        pt = np.abs(np.cos(2 * th)) ** 3                  # the four points: back, sides, front
        front = np.clip(-np.cos(th), 0, 1) ** 4           # the front point, drawn down over the face
        side = np.abs(np.sin(th)) ** 6
        r0 = 0.18
        r1 = 0.24 + 0.12 * pt + 0.04 * side - 0.1 * front
        z0 = 0.66
        z1 = 0.64 - 0.05 * pt - 0.02 * front + 0.02 * side
        f = v ** 0.85
        r = r0 + (r1 - r0) * f
        z = z0 + (z1 - z0) * (0.3 * v + 0.7 * v * v) - (0.008 if lower else 0)
        return np.stack([r * np.sin(th), r * np.cos(th), z], -1)
    return at
k.add("brim", sheet(brim(False), 128, 14, 0.022), BLUE, "body", fuse=False)
k.add("brimunder", sheet(brim(True), 128, 14, 0.012), UNDER, "body", fuse=False)

# --- the crystals: a great one on each upper side, a small one at the front of the ridge ---------------------------
def shine(c, R, h, r):
    """A flat disc of light lying on a crystal of radius R about c, toward h."""
    h = h / np.linalg.norm(h)
    a = np.cross(h, (0, 0, 1.0)) if abs(h[2]) < 0.95 else np.cross(h, (1.0, 0, 0))
    a /= np.linalg.norm(a); b = np.cross(h, a)
    V, F = ell((0, 0, 0), (r, r * 0.75, r * 0.3))
    return V @ np.stack([a, b, h], axis=0) + c + h * R * 0.95, F
for s in (1, -1):
    d = unit(0.55 * s, -0.18, 0.82)
    c = H + d * HR * 0.72
    # Flattened along d so it sits in the bell like a lens, longer front to back
    a = np.cross(d, (0, 1.0, 0)); a /= np.linalg.norm(a); b = np.cross(d, a)
    V, F = ell((0, 0, 0), (0.13, 0.145, 0.09))
    V = V @ np.stack([a, b, d], axis=0) + c
    k.add(f"gem{s}", (V, F), RED, "body", fuse=False, surface="shell")
    k.add(f"gemhi{s}", shine(c, 0.1, d + unit(-0.3 * s, -0.5, 0.4) * 0.8, 0.034), REDHI, "body", fuse=False,
          surface="shell")
d = unit(0, -0.9, 0.42)
c = H + d * HR * 1.02
k.add("gemfront", ell(c, (0.03, 0.03, 0.03)), RED, "body", fuse=False, surface="shell")
k.add("gemfronthi", shine(c, 0.03, d + np.array([0.3, -0.1, 0.5]), 0.011), REDHI, "body", fuse=False,
      surface="shell")

# --- the two beaks: long blue blades hanging from the front of the face ---------------------------------------------
for s, bone in ((1, "tentacle0"), (-1, "tentacle9")):
    p = [(0.03 * s, -0.1, 0.51), (0.042 * s, -0.135, 0.4), (0.055 * s, -0.142, 0.27), (0.072 * s, -0.12, 0.14),
         (0.088 * s, -0.09, 0.08)]
    k.add(f"beak{s}", blade(p, [0.05, 0.044, 0.034, 0.018, 0.005], (1, 0, 0), 0.5), BLUE, bone, fuse=False,
          surface="shell")

# --- the tentacles: grey, out and down from under the face, each ending in a knob ----------------------------------
JOINTS = {i: np.array(p) for i, p in enumerate([(0.037, -0.094, 0.519), (0.097, -0.051, 0.519), (0.12, 0.02, 0.519),
                                                (0.097, 0.09, 0.519), (0.038, 0.134, 0.519), (-0.037, 0.134, 0.519),
                                                (-0.097, 0.091, 0.519), (-0.12, 0.02, 0.519), (-0.097, -0.05, 0.519),
                                                (-0.038, -0.094, 0.519)])}
def tentacle(name, bone, j, reach, drop, out_at, spin=0.0):
    o = np.array([j[0], j[1], 0.0]); o /= np.linalg.norm(o)
    t = np.array([-o[1], o[0], 0.0]) * spin
    z = np.array([0, 0, 1.0])
    p = [j * np.array([0.6, 0.6, 1]) + z * 0.01,
         j + o * reach * 0.35 - z * drop * 0.08 + t * 0.2,
         j + o * reach * 0.75 - z * drop * 0.35 + t * 0.5,
         j + o * reach * 0.9 - z * drop * 0.7 + t * 0.8,
         j + o * reach * out_at - z * drop + t]
    k.add(name, tube(p, [0.024, 0.023, 0.021, 0.02, 0.02]), GREY, bone, fuse=False, surface="skin")
    tip = p[-1] + (p[-1] - p[-2]) / np.linalg.norm(p[-1] - p[-2]) * 0.012
    k.add(name + "knob", ell(tip, (0.03, 0.03, 0.033)), GREY, bone, fuse=False, surface="skin")

LONG = {1: (0.24, 0.43, 1.12, 0.3), 2: (0.27, 0.36, 1.2, -0.2), 3: (0.22, 0.45, 1.05, 0.2), 4: (0.16, 0.42, 1.0, -0.4),
        5: (0.16, 0.44, 1.0, 0.4), 6: (0.22, 0.4, 1.08, -0.2), 7: (0.27, 0.38, 1.2, 0.2), 8: (0.24, 0.45, 1.1, -0.3)}
for i, (reach, drop, out_at, spin) in LONG.items():
    tentacle(f"tentacle{i}", f"tentacle{i}", JOINTS[i], reach, drop, out_at, spin * 0.06)
# A second, shorter tentacle on the front bones beside the beaks, and a long one out sideways on each side
tentacle("tentaclefR", "tentacle0", JOINTS[0] + np.array([0.03, 0.01, 0]), 0.17, 0.4, 1.12, 0.02)
tentacle("tentaclefL", "tentacle9", JOINTS[9] + np.array([-0.03, 0.01, 0]), 0.17, 0.4, 1.12, -0.02)
tentacle("tentaclesR", "tentacle2", JOINTS[2] + np.array([0, -0.02, -0.01]), 0.33, 0.24, 1.08, -0.02)
tentacle("tentaclesL", "tentacle7", JOINTS[7] + np.array([0, -0.02, -0.01]), 0.33, 0.24, 1.08, 0.02)

# --- the eyes: fierce white almonds on the black face, a black pupil toward the middle ------------------------------
CD, CW, CL, CK = srgb(DARK), np.array([1.0, 1.0, 1.0]), srgb("#8A96A8"), srgb(INK)
SHAPE = [(-1.0, -0.25), (-0.4, -0.72), (0.45, -0.68), (1.0, -0.05), (0.85, 0.62), (-0.55, 0.12)]
OUT_R = polygon(SHAPE)
OUT_L = polygon([(-u, v) for u, v in SHAPE])
def white(u, v):
    return np.tile(CW, (len(u), 1))
ED = unit(0.42, -0.9, 0.08)
def eyes(state):
    for s in (1, -1):
        dn = ED * np.array([s, 1, 1])
        e = FC + dn * FR * 0.98
        look = eye_look(state, white, CD, CL, line=0.16)
        col = look if s > 0 else (lambda u, v, f=look: f(-u, v))
        k.decal(e, dn, (0.05, 0.036), col, outline=OUT_R if s > 0 else OUT_L, eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right = np.cross((0, 0, 1.0), dn) * s; right /= np.linalg.norm(right)          # outward
            pc = e - right * 0.013 - np.array([0, 0, 0.004 if state == "Open" else 0.008])
            k.decal(pc, dn, (0.014, 0.015), INK, eps=0.007, rings=8, segs=32)

k.build(voxel=0.0055, smooth=3)
k.eye_states(eyes)
k.export(out)
