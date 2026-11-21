# Klink sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/klink.py -- <the export's Klink.glb> <out.glb>
#
# From the references: two pale lavender-grey gears of seven square-topped teeth, floating, the upper-left one a
# little behind the lower-right one, their teeth meeting. Each gear's front is a dark grey disc with a teal ball
# standing out of its middle, and on the disc a face: one round eye (white, a black rim, a solid black pupil), a
# white plus and a small black oval mouth. The two faces are laid out differently (the upper gear's eye at its upper
# left, the plus at its right, the mouth low at its left; the lower gear's plus at its top, the eye at its left, the
# mouth low at its right). The backs are plain with a dark disc.
#
# Written to be grown from (klang.py and klinklang.py import it with sibling): the colours, the gear, its plate, its
# core and its face are functions of a centre, a radius and a frame; the build is under the main guard.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, srgb, unit, eye_look, args, slab, lathe, polygon

PALE, STEEL, DISC = "#C9CDDA", "#969AA3", "#6B6C73"       # Klink's gears, the great gear of Klang, the face's disc
TEAL, RED = "#2C98A3", "#E86A78"
WHITE, INK, LINE = "#F7F8FA", "#141519", "#E9EAEE"

# A gear's proportions as shares of its tip radius
ROOT = 0.76            # the teeth's root
TOOTH_ROOT, TOOTH_TIP = 0.27, 0.17     # half a tooth's width at its root and at its tip, as shares of the pitch
PLATE_R = 0.63         # the dark disc
CORE_R = 0.27          # the teal ball
PROUD = 0.0015         # how far the dark disc stands out of the gear's face


def frame(n, up=(0, 0, 1)):
    """The gear's plane: ex, ez in it and n out of its front (n = ex x ez, as slab makes it)."""
    n = unit(*n)
    up = np.array(up, float)
    ex = np.cross(up, n); ex /= np.linalg.norm(ex)
    ez = np.cross(n, ex)
    return ex, ez, n


def facing(yaw):
    """A front turned yaw degrees from straight ahead toward +X (the game's front three-quarter camera sees +X)."""
    t = math.radians(yaw)
    return frame((math.sin(t), -math.cos(t), 0))


def outline(r, teeth, turn=0.0):
    """A gear's outline in its plane: square-topped teeth on a round root, the first tooth at turn degrees."""
    pts = []
    pitch = 2 * math.pi / teeth
    for i in range(teeth):
        a = math.radians(turn) + i * pitch
        for da, rr in ((-TOOTH_ROOT, ROOT), (-TOOTH_TIP, 1.0), (TOOTH_TIP, 1.0), (TOOTH_ROOT, ROOT)):
            t = a + da * pitch
            pts.append((r * rr * math.cos(t), r * rr * math.sin(t)))
        for f in (0.5,):              # the root between this tooth and the next
            t = a + (TOOTH_ROOT + f * (1 - 2 * TOOTH_ROOT)) * pitch
            pts.append((r * ROOT * math.cos(t), r * ROOT * math.sin(t)))
    return pts


def at(c, fr, r, deg, rad=0.0):
    """A point of the gear's front plane: rad along the plane's normal, r out at deg from ex toward ez."""
    ex, ez, n = fr
    t = math.radians(deg)
    return np.array(c, float) + ex * r * math.cos(t) + ez * r * math.sin(t) + n * rad


def gear(k, name, bone, c, r, teeth, fr, thick, colour=PALE, turn=0.0):
    """A toothed gear, loose (its edges kept hard), its middle plane through c."""
    ex, ez, n = fr
    k.add(name, slab(outline(r, teeth, turn), c, ex, ez, thick), colour, bone, fuse=False, surface="metal")


def plate(k, name, bone, c, r, fr, thick, back=True, colour=DISC):
    """The dark disc on a gear's front (fused: the face's patches lie on it), and one on its back."""
    ex, ez, n = fr
    t = 0.012 * r / 0.11
    front = np.array(c, float) + n * (thick / 2 + PROUD)
    k.add(name, lathe(front, -n, [(0, 0), (PLATE_R * r, 0), (PLATE_R * r, t), (0, t)], seg=48), colour, bone)
    if back:
        k.add(name + "back", lathe(np.array(c, float) - n * (thick / 2 + PROUD), n,
                                    [(0, 0), (0.55 * r, 0), (0.55 * r, t), (0, t)], seg=48), colour, bone)


def core(k, name, bone, c, r, fr, thick, share=CORE_R, colour=TEAL, squash=1.0):
    """The ball standing out of a gear's middle (loose, so no patch lands on it)."""
    ex, ez, n = fr
    rr = share * r
    m = np.array(c, float) + n * (thick / 2 + 0.25 * rr)
    V, F = ell((0, 0, 0), (rr, rr, rr * squash))
    V = V[:, :1] * ex + V[:, 1:2] * ez + V[:, 2:] * n + m
    k.add(name, (V, F), colour, bone, fuse=False, surface="shell")


def plus(arm, width):
    """A plus's outline, arms of half-width width (a share of the half-length)."""
    w = width
    return polygon([(w, w), (1, w), (1, -w), (w, -w), (w, -1), (-w, -1), (-w, -w), (-1, -w), (-1, w), (-w, w),
                    (-w, 1), (w, 1)])


def turned(fr, deg):
    """The gear plane's ez turned deg degrees about its normal (a patch's up)."""
    ex, ez, n = fr
    t = math.radians(deg)
    return -math.sin(t) * ex + math.cos(t) * ez


# The face: where its three things sit on the disc (degrees from ex toward ez, distance as a share of r)
FACE_RAD = 0.46
EYE_HALF = 0.19           # the eye's half size, a share of r
RIM, PUPIL = 0.76, 0.43   # the eye's rim's inner edge and its pupil, shares of its half size
CW, CI, CL = srgb(WHITE), srgb(INK), srgb(LINE)


def eye_open(u, v):
    c = np.tile(CW, (len(u), 1))
    rr = u * u + v * v
    c[rr > RIM ** 2] = CI
    c[rr < PUPIL ** 2] = CI
    return c


def marks(k, c, r, fr, thick, plus_at, plus_roll, mouth_at, mouth_roll=0.0):
    """The plus and the mouth (every eye state has them: laid before build)."""
    ex, ez, n = fr
    f = thick / 2 + PROUD
    k.decal(at(c, fr, FACE_RAD * r, plus_at, f), n, (0.15 * r, 0.15 * r), WHITE, up=turned(fr, plus_roll),
            outline=plus(0.15, 0.36), rings=14, segs=120, eps=0.0015)
    k.decal(at(c, fr, FACE_RAD * r, mouth_at, f), n, (0.07 * r, 0.1 * r), INK, up=turned(fr, mouth_roll),
            rings=10, segs=40, eps=0.0015)


def eye(k, state, c, r, fr, thick, eye_at):
    ex, ez, n = fr
    k.decal(at(c, fr, FACE_RAD * r, eye_at, thick / 2 + PROUD), n, (EYE_HALF * r, EYE_HALF * r),
            eye_look(state, eye_open, srgb(DISC), CL, line=0.17), up=ez, eps=0.0015, rings=20, segs=56)


def faced_gear(k, name, bone, c, r, teeth, fr, thick, turn, plus_at, plus_roll, mouth_at, mouth_roll):
    """A Klink's gear: the gear, its dark discs, its core and its plus and mouth (the eye is laid by eye())."""
    gear(k, name, bone, c, r, teeth, fr, thick, turn=turn)
    plate(k, name + "plate", bone, c, r, fr, thick)
    core(k, name + "core", bone, c, r, fr, thick)
    marks(k, c, r, fr, thick, plus_at, plus_roll, mouth_at, mouth_roll)


# --- Klink ---------------------------------------------------------------------------------------------------------
R = 0.11
THICK = 0.034
YAW = 12
FR = facing(YAW)
G1 = np.array([-0.075, 0.020, 0.25])        # the upper gear (body bone), a little behind
G2 = np.array([0.085, -0.020, 0.16])        # the lower gear (gear2 bone), in front
# (teeth's turn, eye, plus, plus's roll, mouth, mouth's roll) for each gear, from the pictures
F1 = (8, 132, -12, 28, 222, 30)
F2 = (30, 196, 88, 0, -42, -30)


def klink(src, out):
    k = Kit(src, coat="metal")
    for name, bone, c, (turn, e, p, pr, m, mr) in (("gear1", "body", G1, F1), ("gear2", "gear2", G2, F2)):
        faced_gear(k, name, bone, c, R, 7, FR, THICK, turn, p, pr, m, mr)
    k.build(voxel=0.0035, smooth=3, wband=0.01)

    def lay(state):
        eye(k, state, G1, R, FR, THICK, F1[1])
        eye(k, state, G2, R, FR, THICK, F2[1])
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    src, out = args()
    klink(src, out)
