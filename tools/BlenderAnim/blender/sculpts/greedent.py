# Greedent sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/greedent.py -- <the export's Greedent.glb> <out.glb>
# Skwovet grown fat and upright: a pinkish-tan pear of a body with a dark brown belly from the chest to the feet,
# short thick legs, little fists held together at the chest; the head with a great dark brown beard of cheek fur
# flecked orange, an open mouth with two big buck teeth, an orange nose, upright black oval eyes and Skwovet's
# two-pronged ear tufts; behind, the tail curled up into a huge pink ball as wide as the whole Pokemon, two small
# curls on its top, a dark fern of a fringe up its side.
#
# Grown from skwovet.py (its ear tufts, nose, tail ball and the way of its eyes).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, blade, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args, sibling

sk = sibling("skwovet")

# --- colours ----------------------------------------------------------------------------------------------------------
PINK = "#D9A190"            # the coat and the tail ball
BROWN = "#86432F"           # the belly, the beard, the fringe and the ear tufts
FACE = "#E8BEAA"            # the paler face round the eyes
FLECK = "#E8A562"           # the orange flecks on the beard
ORANGE = sk.ORANGE
INK, MOUTH, TONGUE, TOOTH = "#18141A", "#5A2026", "#E86F7C", "#FFFFFF"

# --- the frame (skeleton: body (0, 0, 0.3), head (0, -0.04, 0.42), arms (+-0.15, -0.08, 0.34),
#     legs (+-0.1, -0.02, 0.12), tail (0, 0.15, 0.23)) ------------------------------------------------------------------
H = np.array([0, -0.075, 0.515])
HR = np.array([0.122, 0.106, 0.112])
BALL = np.array([0, 0.19, 0.55])
BALL_R = np.array([0.245, 0.165, 0.25])


def add_body(k):
    def pear(V):
        V = V.copy(); s = 1 - 0.14 * V[:, 2]
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell((0, -0.01, 0.25), (0.16, 0.14, 0.22), deform=pear), PINK, "body")
    # the belly, dark brown, from the chest to between the feet; its sides in points
    def belly(P):
        z = P[:, 2]
        w = 0.118 - 0.06 * smoothstep(0.2, 0.42, z) + 0.012 * tri(z * 28)
        return inside(np.abs(P[:, 0]) - w, 0.003) * inside(P[:, 1] + 0.05, 0.004) * inside(z - 0.43, 0.004) \
            * inside(0.035 - z, 0.003)
    k.paint(belly, BROWN, on=["body"])
    # short thick legs: haunches fused into the flanks, wide feet
    k.add("haunch", ell((0.1, -0.01, 0.1), (0.075, 0.085, 0.09)), PINK, "leg{s}", mirror=True)
    k.add("foot", ell((0.1, -0.065, 0.022), (0.052, 0.07, 0.022)), PINK, "leg{s}", mirror=True)
    # the arms forward and in, the little fists held together at the chest under the beard
    k.add("arm", tube([(0.14, -0.07, 0.35), (0.12, -0.15, 0.33), (0.07, -0.19, 0.345)], [0.04, 0.035, 0.032]),
          PINK, "arm{s}", mirror=True)
    k.add("fist", ell((0.052, -0.2, 0.355), (0.034, 0.03, 0.036)), PINK, "arm{s}", mirror=True)


def add_head(k):
    k.add("head", ell(H, HR), PINK, "head")
    k.add("neck", ell((0, -0.06, 0.42), (0.1, 0.08, 0.06)), PINK, "body")
    # the beard: a wide dark mass across the lower face, its lower edge in points
    k.add("beard", ell(H + (0, -0.056, -0.082), (0.158, 0.08, 0.085)), BROWN, "head")
    for i, x in enumerate((0.11, 0.06, 0.0)):
        for s in ((1, -1) if x else (1,)):
            b = H + np.array([x * 1.15 * s, -0.085 + abs(x) * 0.25, -0.138])
            t = b + np.array([0.02 * s, -0.006, -0.05 + abs(x) * 0.12])
            k.add(f"beardpt{i}{'R' if s > 0 else 'L'}", blade([b, (b + t) / 2, t], [0.03, 0.02, 0.007], (0, 1, 0), 0.5),
                  BROWN, "head")
    sk.ear_tuft(k, H + (0.072, 0.012, 0.08), 1.3, H, colour=BROWN)


def add_tail(k):
    # the tail curled up into the great ball behind
    k.add("tailball", ell(BALL, BALL_R), PINK, "tail")
    k.add("tailroot", tube([(0, 0.1, 0.16), (0, 0.16, 0.22), (0, 0.2, 0.32)], [0.08, 0.1, 0.12]), PINK, "tail")
    # two small curls on its top
    for s in (1, -1):
        b = BALL + np.array([0.13 * s, 0.0, BALL_R[2] * 0.82])
        k.add(f"curl{'R' if s > 0 else 'L'}", tube([b, b + (0.012 * s, -0.01, 0.05), b + (0.03 * s, -0.035, 0.07),
                                                     b + (0.04 * s, -0.05, 0.055)], [0.03, 0.024, 0.018, 0.013]),
              PINK, "tail")
    # the fern: a dark fringe up the ball's +X side, its fronds out and back
    spine = [(0.12, 0.16, 0.16), (0.2, 0.17, 0.27), (0.25, 0.17, 0.4), (0.27, 0.16, 0.53), (0.25, 0.13, 0.65)]
    k.add("fern", tube(spine, [0.05, 0.06, 0.06, 0.05, 0.035]), BROWN, "tail")
    # broad drooping leaves of fur, alternately out to the side and out behind, their faces to the front and back
    for i, (a, l, back) in enumerate(((0.12, 0.13, 0), (0.27, 0.15, 1), (0.42, 0.16, 0), (0.57, 0.15, 1),
                                      (0.72, 0.13, 0), (0.88, 0.1, 1))):
        P = np.array(spine); j = a * (len(P) - 1); i0 = int(j); f = j - i0
        q = P[i0] * (1 - f) + P[min(i0 + 1, len(P) - 1)] * f
        d = unit(0.8, 0.2 + 0.5 * back, -0.45 + 0.35 * a)
        bend = np.array([0, 0, -0.03])
        k.add(f"frond{i}", blade([q, q + d * l * 0.5, q + d * l + bend], [0.045, 0.034, 0.01], (0, 1, 0), 0.42),
              BROWN, "tail")


def add_face(k):
    # the pale face round the eyes, painted over the upper head's front
    def face(P):
        d = (P - (H + (0, -0.07, 0.034))) / np.array([0.098, 0.068, 0.062])
        return inside(np.linalg.norm(d, axis=1) - 1, 0.004)
    k.paint(face, FACE, on=["head"])
    k.add("nose", ell(H + (0, -0.134, -0.034), (0.024, 0.014, 0.015)), ORANGE, "head", fuse=False, surface="skin")
    # the open mouth with two big buck teeth
    CB, CM, CT, CW = srgb(BROWN), srgb(MOUTH), srgb(TONGUE), srgb(TOOTH)
    def mouth(u, v):
        c = np.tile(CM, (len(u), 1))
        c[(u / 0.7) ** 2 + ((v + 0.85) / 0.6) ** 2 < 1] = CT
        c[(np.abs(u) < 0.36) & (np.abs(u) > 0.03) & (v > -0.15)] = CW
        return c
    def mouth_shape(th):
        s = math.sin(th)
        return min(1.0, 0.55 / s) if s > 0 else 1.0
    k.decal(H + (0, -0.134, -0.08), unit(0, -1, -0.15), (0.046, 0.048), mouth, eps=0.003, outline=mouth_shape,
            rings=20, segs=64)
    # orange flecks on the beard, three to a side
    for x, z in ((0.07, -0.05), (0.095, -0.075), (0.065, -0.085)):
        for s in (1, -1):
            p = H + np.array([x * 1.15 * s, -0.145 + x * 0.35, z * 1.12])
            k.decal(p, unit(0.6 * s, -1, 0.1), (0.008, 0.008), FLECK, eps=0.003, rings=5, segs=20)


def eye_colour():
    CI = srgb(INK)
    def eye(u, v):
        c = np.tile(CI, (len(u), 1))
        c[np.hypot((u - 0.2) / 0.3, (v - 0.35) / 0.26) < 1] = (1, 1, 1)
        return c
    return eye


if __name__ == "__main__":
    src, out = args()
    k = Kit(src, coat="fur")
    add_body(k)
    add_head(k)
    add_tail(k)
    add_face(k)
    k.build(voxel=0.0075, band=0.004)
    k.eye_states(lambda s: sk.lay_eyes(k, s, x=0.042, dz=0.034, half=(0.021, 0.031), lid=FACE, h=H, hr=HR,
                                       look=eye_colour()))
    k.export(out)
