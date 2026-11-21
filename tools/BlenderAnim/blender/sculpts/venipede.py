# Venipede sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/venipede.py -- <the export's Venipede.glb> <out.glb>
#
# From the references: a small centipede whose front two thirds are a great magenta dome of a carapace, behind which a
# lower dark green abdomen of segments shows, black lines between them. The face at the dome's front: a ridge running
# down the middle to a point between two round cheek lobes, a yellow-orange half-moon eye under each lobe (its top cut
# flat, a dark rim and a dark pupil toward the nose), a dark grey V on the brow, two dark grey fangs pointing down
# under the face. A pair of antennae rising up, out and forward from the brow and a pair rising up and back from the
# tail: magenta cones with a dark grey band. A dark ring with a magenta middle on each side of the dome. Short dark
# grey spiky legs down both sides under the body.
# Skeleton: tail and body (0, 0.17, 0.07), seg1 (0, 0.1, 0.075), seg2 (0, 0.03, 0.08), seg3 (0, -0.04, 0.085),
# head (0, -0.08, 0.095).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, hull, lathe, srgb, inside, unit, eye_look, polygon, args

RED, DOMEDARK, GREEN, LINE = "#C4325C", "#9E2A4C", "#2F6A44", "#1E2A22"
GREY, INK, YELLOW, PUPIL = "#4A464E", "#221C22", "#F2A63A", "#1A1418"

DOME_C, DOME_R = np.array([0.0, -0.02, 0.118]), np.array([0.14, 0.168, 0.13])
LOBE_C, LOBE_R = np.array([0.064, -0.162, 0.074]), np.array([0.062, 0.058, 0.056])
GREENS = [((0, 0.12, 0.096), (0.108, 0.052, 0.094)), ((0, 0.175, 0.09), (0.096, 0.046, 0.084)),
          ((0, 0.226, 0.084), (0.08, 0.04, 0.07)), ((0, 0.268, 0.08), (0.06, 0.034, 0.054))]
SEAMS = [0.148, 0.202, 0.25]


def on_ell(c, r, d):
    c, r, d = np.array(c, float), np.array(r, float), unit(*d)
    p = c + d / math.sqrt(((d / r) ** 2).sum())
    n = (p - c) / r ** 2
    return p, n / np.linalg.norm(n)


def bone_of(y):
    if y < -0.06:
        return "head"
    if y < 0.0:
        return "seg3"
    if y < 0.065:
        return "seg2"
    if y < 0.135:
        return "seg1"
    if y < 0.2:
        return "body"
    return "tail"


def flat_bottom(lim):
    def f(V):
        V = V.copy()
        V[:, 2] = np.maximum(V[:, 2], lim)
        return V
    return f


def antenna(k, name, base, d, length, r0, bone, color=RED, band=GREY, bands=((0.34, 0.6),)):
    """A cone along d with dark bands round it: an antenna, a horn, a prong."""
    def rad(t):
        return r0 * (1 - t) ** 0.75 + 0.0015
    prof = [(0, 0)] + [(rad(t), t * length) for t in np.linspace(0, 0.97, 12)] + [(0, length)]
    k.add(name, lathe(base, d, prof, seg=24), color, bone, fuse=False, surface="shell")
    for i, (t0, t1) in enumerate(bands):
        pb = [(0, t0 * length)] + [(rad(t) + 0.0028, t * length) for t in np.linspace(t0, t1, 5)] + [(0, t1 * length)]
        k.add(f"{name}b{i}", lathe(base, d, pb, seg=24), band, bone, fuse=False, surface="shell")


def spike(k, name, base, tip, w, bone, color=GREY, mirror=True, along=(0, 1, 0)):
    """A short solid spike from a small base round base to tip (a leg, a fang): a flat-faced hull."""
    base, tip = np.array(base, float), np.array(tip, float)
    d = tip - base; d /= np.linalg.norm(d)
    a = np.cross(d, along); a /= np.linalg.norm(a)
    b = np.cross(d, a)
    pts = [base + a * w, base - a * w, base + b * w * 0.8, base - b * w * 0.8, base - d * w * 0.5, tip]
    k.add(name, hull(pts), color, bone, fuse=False, mirror=mirror, surface="shell")


def body(k):
    k.add("dome", ell(DOME_C, DOME_R, deform=flat_bottom(-0.7)), RED, "seg2")
    k.add("domefront", ell((0, -0.1, 0.11), (0.125, 0.09, 0.11), deform=flat_bottom(-0.7)), RED, "seg3")
    k.add("lobe", ell(LOBE_C, LOBE_R), RED, "head", mirror=True)

    def beak(V):
        V = V.copy()
        V[:, 0] *= 0.45 + 0.55 * (V[:, 2] + 1) / 2
        return V
    k.add("ridge", ell((0, -0.172, 0.112), (0.036, 0.05, 0.08), rot=(-14, 0, 0), deform=beak), RED, "head")
    for i, (c, r) in enumerate(GREENS):
        k.add(f"green{i}", ell(c, r, deform=flat_bottom(-0.75)), GREEN, bone_of(c[1]))
    # the black lines between the green segments
    for y in SEAMS:
        k.paint(lambda P, y=y: inside(np.abs(P[:, 1] - y) - 0.0045, 0.002) * (P[:, 2] > 0.03), LINE, on=["green"])


def marks(k):
    # a dark ring with a magenta middle on each side of the dome
    CR, CG = srgb(RED), srgb(INK)
    def ring(u, v):
        r = np.sqrt(u * u + v * v)
        c = np.tile(CR, (len(u), 1))
        c[(r > 0.48) & (r < 0.92)] = CG
        return c
    for s in (1, -1):
        p, n = on_ell(DOME_C, DOME_R, (0.92 * s, 0.42, 0.05))
        k.decal(p, n, (0.046, 0.046), ring, eps=0.004, rings=16, segs=48)
    # the dark V on the brow, from the antennae's roots down to the ridge
    n = unit(0, -0.72, 0.7)
    for s in (1, -1):
        a = np.array((0.054 * s, -0.152, 0.2)); b = np.array((0.0, -0.194, 0.148))
        mid = (a + b) / 2; dd = b - a; L = np.linalg.norm(dd) / 2 + 0.006
        k.decal(mid, n, (L, 0.0085), INK, up=np.cross(n, dd / np.linalg.norm(dd)), eps=0.004, rings=6, segs=32)


def pieces(k):
    # antennae: two from the brow up, out and forward, two from the tail up and back
    antenna(k, "antF", (0.05, -0.14, 0.205), unit(0.5, -0.55, 0.68), 0.14, 0.021, "head")
    antenna(k, "antF", (-0.05, -0.14, 0.205), unit(-0.5, -0.55, 0.68), 0.14, 0.021, "head")
    antenna(k, "antB", (0.035, 0.26, 0.11), unit(0.35, 0.6, 0.72), 0.13, 0.019, "tail")
    antenna(k, "antB", (-0.035, 0.26, 0.11), unit(-0.35, 0.6, 0.72), 0.13, 0.019, "tail")
    # legs: short dark spikes down both sides
    for i, y in enumerate((-0.11, -0.05, 0.01, 0.07, 0.13, 0.19, 0.24)):
        x = 0.085 if y < 0.13 else 0.07 - (y - 0.13) * 0.3
        spike(k, f"leg{i}", (x, y, 0.04), (x + 0.03, y - 0.012, 0.0), 0.017, bone_of(y))
    # fangs: two dark points down under the face
    spike(k, "fang", (0.03, -0.192, 0.045), (0.022, -0.225, 0.002), 0.014, "head", along=(1, 0, 0))


# ---- the eyes: a yellow-orange half moon under each cheek lobe, its top cut flat, a dark rim, a dark pupil inward
CUT = 0.32
EYE = []
for th in np.linspace(0, 2 * math.pi, 96, endpoint=False):
    u, v = math.cos(th), math.sin(th)
    if v > CUT:
        t = CUT / v
        u, v = u * t, v * t
    EYE.append((u, v))
EYER = polygon(EYE)
CY, CK, CP, CL = srgb(YELLOW), srgb(INK), srgb(PUPIL), srgb(RED)


def eye_open(u, v):
    reach = np.array([EYER(t) for t in np.arctan2(v, u)])
    rho = np.sqrt(u * u + v * v) / reach
    c = np.tile(CY, (len(u), 1))
    c[rho > 0.8] = CK
    return c


EYE_HALF = (0.042, 0.04)


def eyes(k, state, lobe_c=LOBE_C, lobe_r=LOBE_R, half=EYE_HALF, d=(0.42, -0.8, -0.3)):
    look = eye_look(state, eye_open, CL, CK, line=0.17)
    for s in (1, -1):
        p, n = on_ell(lobe_c * np.array([s, 1, 1]), lobe_r, (d[0] * s, d[1], d[2]))
        col = look if s == 1 else (lambda a, b, f=look: f(-a, b))
        k.decal(p, n, half, col, eps=0.004, rings=20, segs=56, outline=EYER)
        if state in ("Open", "Fierce"):
            # the pupil: a dark upright oval toward the nose, a patch of its own over the eye
            r_ = np.cross((0, 0, 1.0), n); r_ /= np.linalg.norm(r_)
            pc = p - r_ * half[0] * 0.2 * s - np.array((0, 0, half[1] * 0.08))
            h = (half[0] * 0.24, half[1] * 0.4) if state == "Open" else (half[0] * 0.22, half[1] * 0.26)
            if state == "Fierce":
                pc = pc - np.array((0, 0, half[1] * 0.22))
            k.decal(pc, n, h, PUPIL, eps=0.007, rings=6, segs=28)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="shell")
    body(k)
    marks(k)
    pieces(k)
    k.build(voxel=0.005, smooth=3, band=0.004)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)
