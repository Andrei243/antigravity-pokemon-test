# Chespin sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/chespin.py -- <the export's Chespin.glb> <out.glb>
# Written to be grown from: quilladin.py and chesnaught.py import its frame (sibling("chespin")): the line's colours,
# the soft quill, the eye, the nose, the open mouth and the way a face is painted into a hood.
#
# From the references: a small tan chestnut of a Pokemon standing upright. A big green hood over its head, open at the
# face and carried down the back as a green stripe to the tail; five soft pointed quills on the hood's top (two out to
# the sides, two up, one tall in the middle) and a little spur at each lower side of the hood; a big tan face: a brown
# wedge on the forehead pointing down between the eyes, a brown mark on each cheek at the hood's edge, small black oval
# eyes with a white glint, a small salmon triangle nose, a wide open D-shaped mouth (white upper teeth, pink tongue);
# a pear-shaped tan body; flat brown paddle arms; short tan legs with a brown cuff and big pointed white feet; a green
# tail with an orange tip.
# (The pictures overrule the task's text: five quills, not three; no green on the face or the belly.)
# Reference ratios (3D picture, feet to the middle quill's tip): hood (top to chin) / height 0.43; quills above the
# hood / height 0.16; hood width / height 0.43; body width at the hips / height 0.2; legs showing / height 0.13.
# The export's joints: body (0, 0, 0.1), arms (+-0.045, -0.01, 0.14), head (0, -0.01, 0.15), legs (+-0.03, 0, 0.06),
# tail (0, 0.04, 0.07).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, boxy, eye_look, args, outward, axes, polygon, hull

# --- colours (the line's) -----------------------------------------------------------------------------------------------
GREEN = "#9CCB4C"
TAN = "#E3AE78"
BROWN = "#7A4C2E"
NOSE = "#EE7458"
MOUTH = "#7E2C42"
TONGUE = "#E5788C"
WHITE = "#F6F6F6"
ORANGE = "#E8693A"
INK = "#1E1614"

# --- the frame ----------------------------------------------------------------------------------------------------------
H = np.array([0.0, -0.01, 0.228])           # the hood's middle
HR = np.array([0.086, 0.08, 0.081])
FACE = np.array([0.0, -0.04, 0.208])        # the face's bulge out of the hood's front
FACER = np.array([0.07, 0.055, 0.062])
FACE_Z, FACE_HALF = 0.214, (0.064, 0.067)   # the tan oval painted on the front
EYE_X, EYE_Z, EYE_HALF = 0.031, 0.228, (0.0115, 0.0165)
SH = np.array([0.045, -0.01, 0.14])
HAND = np.array([0.064, -0.068, 0.112])


def on_ell(x, z, c, r, lift=0.0):
    """The front point of an ellipsoid at (x, z) and its outward normal."""
    zn = (z - c[2]) / r[2]
    q = max(0.0, 1 - zn * zn - (x / r[0]) ** 2)
    p = np.array([x, c[1] - r[1] * math.sqrt(q), z])
    n = (p - c) / np.array(r) ** 2; n /= np.linalg.norm(n)
    return p + n * lift, n


def quill(k, name, base, d, length, r, color, bone, bend=0.15, fuse=True):
    """A soft pointed quill: a leaf-shaped tube out of a surface along d, its tip bent a little further out."""
    base = np.array(base, float); d = unit(*d)
    side = np.cross(d, (0, 0, 1.0))
    side = side / np.linalg.norm(side) if np.linalg.norm(side) > 1e-3 else np.array([1.0, 0, 0])
    bendto = unit(*(d + np.array([np.sign(d[0]) * bend, 0, 0])))
    pts = [base - d * r * 0.6, base + d * length * 0.35, base + d * length * 0.7, base + bendto * length * 0.98]
    k.add(name, tube(pts, [r, r * 1.12, r * 0.75, r * 0.22], seg=18, per=6), color, bone, fuse=fuse)


def black_eye(ink=INK, white=WHITE, glint=(0.28, 0.4, 0.3)):
    """A solid dark oval with a white glint toward its outer top."""
    I_, W_ = srgb(ink), srgb(white)
    gu, gv, gr = glint
    def look(u, v):
        c = np.tile(I_, (len(u), 1))
        c[((u - gu) / gr) ** 2 + ((v - gv) / (gr * 1.2)) ** 2 < 1] = W_
        return c
    return look


def d_shape(top=0.45):
    """A D's outline: a flat top at v = top over the round bottom of the unit circle (the mouth's own edge)."""
    def r(th):
        s = math.sin(th)
        return min(1.0, top / s) if s > 1e-6 else 1.0
    return r


def open_mouth(k, p, n, half, mouth=MOUTH, tongue=TONGUE, teeth=WHITE, top=0.45, eps=0.0018, bone_up=(0, 0, 1)):
    """
    A wide open D-shaped mouth: its outline the patch's own edge (crisp), a pink tongue in its bottom, and the upper
    teeth a white band laid as a patch of its own over its flat top.
    """
    M_, T_ = srgb(mouth), srgb(tongue)
    def look(u, v):
        c = np.tile(M_, (len(u), 1))
        c[(u / 0.6) ** 2 + ((v + 0.7) / 0.5) ** 2 < 1] = T_
        return c
    k.decal(p, n, half, look, eps=eps, rings=26, segs=72, outline=d_shape(top))
    up = np.array(bone_up, float)
    t = np.array(p, float) + up * half[1] * (top - 0.17)
    k.decal(t, n, (half[0] * 0.7, half[1] * 0.17), teeth, eps=eps * 1.6, rings=6, segs=48,
            outline=polygon([(-1, 1), (1, 1), (0.92, -1), (-0.92, -1)]))


def nose(k, p, n, size, color=NOSE, bone="head", name="nose"):
    """A small rounded triangle of a nose, wide above and pointed below, standing on the face."""
    def tri_(V):
        V = V.copy()
        V[:, 0] *= 0.62 + 0.38 * V[:, 2]
        return V
    c = np.array(p, float) + np.array(n, float) * size[1] * 0.45
    k.add(name, outward(ell(c, size, rot=(-10, 0, 0), deform=tri_, sub=3)), color, bone, fuse=False, surface="skin")


# --- Chespin ------------------------------------------------------------------------------------------------------------
def add_hood(k):
    k.add("hood", ell(H, HR), GREEN, "head")
    k.add("face", ell(FACE, FACER), GREEN, "head")          # painted tan below
    k.add("nape", ell((0, 0.02, 0.165), (0.06, 0.05, 0.035)), GREEN, "head")
    # the tan face, crisp at the hood's rim; the chin is tan to the bottom
    def face(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        e = (x / FACE_HALF[0]) ** 2 + (np.minimum(z - FACE_Z, 0.0) / 0.075) ** 2 + (np.maximum(z - FACE_Z, 0.0) / FACE_HALF[1]) ** 2
        return inside(e - 1, 0.03) * (y < -0.035)
    k.paint(face, TAN)
    # five quills on the top: out to each side, up, and the tallest in the middle
    quill(k, "quillmid", (0, 0.0, 0.303), (0, 0.12, 1), 0.068, 0.0155, GREEN, "head", bend=0.0)
    for side, s in (("R", 1), ("L", -1)):
        quill(k, "quillin" + side, (0.035 * s, 0.002, 0.296), (0.38 * s, 0.12, 1), 0.062, 0.0148, GREEN, "head")
        quill(k, "quillout" + side, (0.066 * s, 0.0, 0.272), (1.0 * s, 0.08, 0.8), 0.064, 0.0142, GREEN, "head")
        quill(k, "spur" + side, (0.074 * s, -0.012, 0.172), (1.0 * s, -0.25, -0.55), 0.03, 0.0105, GREEN, "head", bend=0.0)


def add_face_marks(k):
    """The brown wedge on the forehead and a mark on each cheek at the hood's edge."""
    p, n = on_ell(0.0, 0.258, FACE, FACER)
    k.decal(p, unit(n[0], n[1], n[2] * 0.7), (0.021, 0.02), BROWN, eps=0.0016, rings=12, segs=48,
            outline=polygon([(-1, 0.85), (1, 0.85), (0.08, -1), (-0.08, -1)]))
    p, n = on_ell(0.056, 0.196, FACE, FACER)
    k.decal(p, unit(n[0], n[1], n[2] * 0.5), (0.011, 0.019), BROWN, eps=0.0016, rings=10, segs=40, mirror=True,
            outline=polygon([(-0.4, 1), (0.9, 0.6), (0.7, -1), (-0.8, -0.7)]))


def add_mouth(k):
    p, n = on_ell(0.0, 0.181, FACE, FACER)
    open_mouth(k, p, unit(0, n[1], n[2] * 0.5), (0.034, 0.026))
    q, m = on_ell(0.0, 0.211, FACE, FACER)
    nose(k, q, m, (0.0125, 0.009, 0.01))


def add_body(k):
    k.add("torso", ell((0, 0.002, 0.098), (0.042, 0.038, 0.058)), TAN, "body", surface="fur")
    k.add("belly", ell((0, -0.004, 0.072), (0.048, 0.043, 0.042)), TAN, "body")
    # the hood's green carried down the back to the tail
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.019 - 0.1 * np.clip(P[:, 2] - 0.12, 0, 1), 0.0015)
            * inside(0.012 - P[:, 1], 0.0015) * (P[:, 2] > 0.045) * (P[:, 2] < 0.175), GREEN)


def add_legs(k):
    k.add("thigh", ell((0.03, 0.0, 0.058), (0.026, 0.026, 0.026)), TAN, "leg{s}", mirror=True)
    k.add("leg", tube([(0.031, 0.0, 0.05), (0.034, -0.004, 0.024)], [0.019, 0.017]), TAN, "leg{s}", mirror=True)
    def clog(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)                     # toward the front the foot narrows to a point
        V[:, 0] *= 1 - 0.55 * t ** 1.5; V[:, 2] *= 1 - 0.4 * t ** 1.5
        return V
    k.add("foot", ell((0.036, -0.018, 0.012), (0.019, 0.036, 0.0135), rot=(0, 0, 14), deform=clog), WHITE, "leg{s}",
          mirror=True, surface="shell")
    leg = lambda P: (np.abs(np.abs(P[:, 0]) - 0.033) < 0.03) * (P[:, 1] > -0.03)
    k.paint(lambda P: inside(0.021 - P[:, 2], 0.0012) * leg(P), WHITE, on=["leg", "foot"])
    k.paint(lambda P: inside(P[:, 2] - 0.021, 0.0012) * inside(0.0295 - P[:, 2], 0.0012) * leg(P), BROWN,
            on=["leg", "foot"])


def add_arms(k):
    """Flat brown paddles held forward and a little down."""
    d = unit(*(HAND - SH))
    k.add("upper", tube([SH, SH + (HAND - SH) * 0.55], [0.0145, 0.0135]), BROWN, "arm{s}", mirror=True)
    a, b = axes(d)
    V, F = ell((0, 0, 0), (0.024, 0.0125, 0.034), sub=4)
    V = V[:, :1] * a + V[:, 1:2] * b + V[:, 2:] * d
    k.add("paddle", (V + HAND, F), BROWN, "arm{s}", mirror=True)


def add_tail(k):
    pts = [(0, 0.03, 0.07), (0.008, 0.068, 0.058), (0.024, 0.096, 0.066), (0.038, 0.113, 0.088)]
    k.add("tail", tube(pts, [0.0135, 0.0125, 0.0105, 0.0075]), GREEN, "tail")
    k.paint(lambda P: inside(0.1 - P[:, 1], 0.0012) * (P[:, 0] > 0.01), ORANGE, on=["tail"])
    tip = np.array(pts[-1]); d = unit(0.45, 0.6, 0.65)
    a, b = axes(d)
    for i, (da, db) in enumerate(((1, 0.3), (-0.8, 0.6), (0.1, -1))):
        q = tip + (a * da + b * db) * 0.004
        k.add(f"tailtip{i}", hull([q + a * 0.006, q - a * 0.006, q + b * 0.006, q - b * 0.006, q - d * 0.006,
                                   q + d * 0.018 + (a * da + b * db) * 0.006]), ORANGE, "tail", fuse=False,
              surface="fur")


def lay_eyes(k, state):
    look = eye_look(state, black_eye(), srgb(TAN), srgb(INK), line=0.2)
    p, n = on_ell(EYE_X, EYE_Z, FACE, FACER)
    k.decal(p, unit(n[0] * 0.8, n[1], n[2] * 0.6), EYE_HALF, look, eps=0.0018, rings=20, segs=56, mirror=True)


def chespin(src, out):
    k = Kit(src, coat="fur")
    add_hood(k)
    add_face_marks(k)
    add_mouth(k)
    add_body(k)
    add_legs(k)
    add_arms(k)
    add_tail(k)
    k.build(voxel=0.0036, smooth=4, band=0.004, wband=0.025)
    k.eye_states(lambda s: lay_eyes(k, s))
    k.export(out)


if __name__ == "__main__":
    chespin(*args())
