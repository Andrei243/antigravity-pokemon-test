# Cofagrigus sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cofagrigus.py -- <the export's Cofagrigus.glb> <out.glb>
#
# Grown from Yamask's script: its gold, its black of shadow, its red eyes (a red disc ringed darker with a black pupil)
# and its mask's face, which is the small gold face on Cofagrigus's crown.
# The design, from the references: a golden sarcophagus floating upright, wide at its shoulders and narrowing to a
# rounded foot, grooves running down its front. Its top is a blue headdress banded with gold, its lappets falling down
# both sides; on the crown a small gold face (Yamask's mask). Under it the true face, dark in a recess: two red eyes
# under a scowl and a wide grin of white pointed teeth; under the grin a blue collar with gold points. Four wavy black
# shadow arms from its back, the upper pair reaching up and out, the lower down and out, each to a great open hand of
# long fingers. The hands are carried in a little from the art's spread so the model is not much wider than it is tall.
# The skeleton: body (0, 0, 0.518), head (0, -0.02, 0.718), arms (+-0.08, 0.07, 0.738), lowArms (+-0.08, 0.07, 0.378).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import (Kit, ell, tube, srgb, unit, smoothstep, inside, eye_look, args, boxy, hull, polygon, tri,
                       sibling)

ya = sibling("yamask")

# --- colours ----------------------------------------------------------------------------------------------------------
GOLD, GOLDDEEP, GOLDLINE = ya.GOLD, ya.GOLDDEEP, "#9A6E30"
BLUE, BLUEDEEP = "#2C78B0", "#1E5C8C"
SHADOW, FACE, TEETH = "#4A4B52", "#26232C", "#F4F2EC"

# --- the frame --------------------------------------------------------------------------------------------------------
BOX_C, BOX_R = np.array([0.0, 0.0, 0.47]), np.array([0.15, 0.092, 0.33])
DRESS_C, DRESS_R = np.array([0.0, 0.002, 0.785]), np.array([0.172, 0.112, 0.118])
FACE_C, FACE_R = np.array([0.0, -0.072, 0.74]), np.array([0.088, 0.05, 0.068])
EYE_X, EYE_Z, EYE_HALF = 0.042, 0.765, 0.026
VOXEL = 0.0065


def coffin_shape(V):
    """A squared ellipsoid widened toward its top: the sarcophagus."""
    V = boxy(3.2)(V) * np.linalg.norm(V, axis=1, keepdims=True)
    V = V.copy()
    s = 1 + 0.2 * V[:, 2]
    V[:, 0] *= s
    V[:, 1] *= 1 + 0.08 * V[:, 2]
    return V


def coffin(k):
    k.add("box", ell(BOX_C, BOX_R, deform=coffin_shape), GOLD, "body", surface="metal")
    # the foot: rounded and a little proud, under a band
    k.add("foot", ell((0, 0.0, 0.2), (0.1, 0.085, 0.07), deform=boxy(3)), GOLD, "body", surface="metal")
    # grooves down the front and bands across it, a darker gold, crisp
    def grooves(P):
        front = (P[:, 1] < -0.04).astype(float)
        down = inside(P[:, 2] - 0.6, 0.002) * inside(0.2 - P[:, 2], 0.002)
        m = np.zeros(len(P))
        for x in (-0.055, 0.0, 0.055):
            m = np.maximum(m, inside(np.abs(P[:, 0] - x) - 0.004, 0.002) * down)
        for z in (0.62, 0.245):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - 0.004, 0.002))
        return m * front * (P[:, 2] < 0.64)
    k.paint(grooves, GOLDDEEP, on=["box", "foot"])


def headdress(k):
    """The blue headdress banded with gold over the coffin's top, its lappets falling down both sides."""
    k.add("dress", ell(DRESS_C, DRESS_R, deform=boxy(2.6)), BLUE, "head", surface="cloth")
    k.add("lappet", ell((0.15, 0.0, 0.675), (0.045, 0.088, 0.1), rot=(0, 8, 0)), BLUE, "head", mirror=True,
          surface="cloth")
    k.add("collar", ell((0, -0.04, 0.655), (0.13, 0.06, 0.03), deform=boxy(3)), BLUE, "head", surface="cloth")
    # gold bands across the dome and down the lappets
    def bands(P):
        m = np.zeros(len(P))
        for z, w in ((0.84, 0.016), (0.705, 0.013), (0.64, 0.011)):
            m = np.maximum(m, inside(np.abs(P[:, 2] - z) - w, 0.002))
        return m
    k.paint(bands, GOLD, on=["dress", "lappet"])
    # gold points round the collar
    def points(P):
        t = P[:, 0] / 0.05
        edge = 0.645 + 0.02 * tri(t + 0.5)
        return inside(P[:, 2] - edge, 0.002) * (P[:, 1] < -0.02)
    k.paint(points, GOLD, on=["collar"])


def face(k):
    """The dark recess under the headdress's brow, with the small gold face on the crown above it."""
    k.add("recess", ell(FACE_C, FACE_R, deform=boxy(2.4)), FACE, "head", surface="skin")
    k.add("crownface", ell((0, -0.098, 0.855), (0.036, 0.022, 0.04)), GOLD, "head", surface="metal")
    k.decal((0, -0.12, 0.856), (0, -1.0, 0.1), (0.03, 0.034), ya.mask_face(GOLD, GOLDLINE, W=0.075), eps=0.002,
            rings=40, segs=140, surface="metal")
    # the grin: a wide band curving up at its corners, white teeth meeting in a zigzag
    teeth, dark = srgb(TEETH), srgb(FACE)
    def grin(u, v):
        vc = -0.3 + 0.55 * u * u
        h = 0.32 * (1 - u * u) + 0.12
        c = np.tile(dark, (len(u), 1))
        band = np.abs(v - vc) < h
        c[band] = teeth
        c[band & (np.abs(v - vc - 0.55 * h * tri(3.5 * u)) < 0.075)] = dark
        return c
    def grin_outline(th):
        u, v = math.cos(th), math.sin(th)
        r = 0.02
        while r < 1.0:
            uu, vv = u * r, v * r
            if abs(vv - (-0.3 + 0.55 * uu * uu)) > 0.32 * (1 - uu * uu) + 0.12:
                break
            r += 0.01
        return max(r, 0.05)
    k.decal((0, -0.122, 0.705), (0, -1.0, 0.0), (0.07, 0.04), grin, eps=0.002, outline=grin_outline, rings=24, segs=120)


HAND = 1.45                       # the hands' size against the first draft: the art's hands are as long as the face


def shadow_arm(k, name, bone, start, path, hand, d, n, s):
    """A wavy shadow arm from the coffin's back to a great open hand: a palm and five long fingers fanned along d."""
    k.add(name, tube([start] + path + [hand - d * 0.03], [0.03] + [0.023] * len(path) + [0.026]), SHADOW, bone)
    side = np.cross(n, d)                       # across the palm
    g = HAND
    pts = []
    for a in (0.0, 0.07 * g):
        for b in (-0.05 * g, 0.05 * g):
            for t in (-0.016, 0.016):
                pts.append(hand + d * a + side * b * (1 - 0.12 * a / (0.07 * g)) + n * t)
    k.add(name + "palm", hull(pts), SHADOW, bone, fuse=False, surface="skin")
    # four fingers and a thumb, fanned, each a tapering prism
    for j, (ang, ln) in enumerate(((-0.4, 0.1), (-0.13, 0.12), (0.13, 0.115), (0.38, 0.095), (1.15 * s, 0.07))):
        fd = d * math.cos(ang) + side * math.sin(ang)
        fs = np.cross(n, fd)
        root = hand + d * 0.055 * g + side * (0.038 * g * math.sin(ang) * 1.3) - d * (0.04 * g if j == 4 else 0.0)
        tip = root + fd * ln * g
        q = []
        for p, w, t in ((root, 0.023, 0.015), (tip, 0.012, 0.011)):
            for b in (-w, w):
                for tt in (-t, t):
                    q.append(p + fs * b + n * tt)
        q.append(tip + fd * 0.016)
        k.add(f"{name}finger{j}", hull(q), SHADOW, bone, fuse=False, surface="skin")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1.0])
        # the upper pair: back and out, then up to hands reaching up and out, palms to the front
        H = np.array([0.33, -0.02, 0.84]) * m
        d = unit(0.62 * s, -0.12, 0.78)
        n = unit(0.15 * s, -1, 0.05); n = n - d * (n @ d); n /= np.linalg.norm(n)
        shadow_arm(k, f"upper{side}", f"arm{side}", np.array([0.07, 0.05, 0.74]) * m,
                   [np.array(p) * m for p in ((0.17, 0.09, 0.75), (0.23, 0.06, 0.8), (0.27, 0.02, 0.8))],
                   H, d, n, s)
        # the lower pair: out and down to hands reaching down and out
        H2 = np.array([0.33, -0.04, 0.27]) * m
        d2 = unit(0.6 * s, -0.15, -0.78)
        n2 = unit(0.2 * s, -1, -0.1); n2 = n2 - d2 * (n2 @ d2); n2 /= np.linalg.norm(n2)
        shadow_arm(k, f"lower{side}", f"lowArm{side}", np.array([0.07, 0.05, 0.38]) * m,
                   [np.array(p) * m for p in ((0.18, 0.09, 0.37), (0.23, 0.05, 0.33), (0.28, 0.0, 0.32))],
                   H2, d2, n2, s)


def lay_eyes(k, state):
    look = eye_look(state, ya.red_eye(tail=False, pupil=0.36), srgb(FACE), srgb(ya.RED), line=0.16)
    for s in (1, -1):
        c = np.array([EYE_X * s, -0.12, EYE_Z])
        col = look if s == 1 else (lambda u, v, f=look: f(-u, v))
        k.decal(c, unit(0.25 * s, -1, 0.0), (EYE_HALF, EYE_HALF), col, eps=0.003, rings=20, segs=56)


def scowl(k):
    """A dark brow slanting down to the middle over each eye, in every state (the art's angry glare)."""
    for s in (1, -1):
        out = polygon([(-1, -0.6), (1, 0.1), (1, 0.65), (-1, 0.0)])
        k.decal((EYE_X * s, -0.122, EYE_Z + 0.024), unit(0.25 * s, -1, 0.0), (0.034, 0.02), FACE, eps=0.005,
                outline=(out if s == 1 else (lambda th, o=out: o(math.pi - th))), rings=8, segs=60)


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="metal")
    coffin(k)
    headdress(k)
    face(k)
    arms(k)
    k.build(voxel=VOXEL, smooth=4, band=0.005)
    k.eye_states(lambda state: (lay_eyes(k, state), scowl(k)))
    k.export(out)
