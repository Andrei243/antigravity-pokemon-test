# Poipole sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/poipole.py -- <the export's Poipole.glb> <out.glb>
#
# From the references: a small purple imp, floating, nearly all head. The head is a great purple bulb narrowing up
# to a pale lavender needle on its top, a magenta crown of points round the needle's foot and a magenta diamond on
# the brow; two rounded lobes hang at the head's lower sides like a hood held round the face, each tipped magenta at
# its upper outer corner with a short pale needle there pointing up and out. Low on the front, between the lobes, a
# magenta mask with two angular cyan eyes (their outer corners swept up) and a small dark smile. Under the chin a
# thin neck banded magenta, a small purple trunk, tiny thin arms (one raised to the chin in the art; here both
# forward and down), two folded legs with round haunches, and a thin tail hanging down and curling to the +X side,
# banded and tipped magenta. Written to be grown from (naganadel.py imports it with sibling): colours, the eye and
# the needle are names here.
# Skeleton: body (0, 0, 0.229), head (0, 0, 0.249), legs (+-0.015, 0, 0.179), tail (0, 0.02, 0.179). No arm bones:
# the arms go on the body.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, lathe, srgb, inside, smoothstep, unit, eye_look, polygon, tri, args

PURPLE, MAGENTA, NEEDLE, CYAN = "#7A4FC2", "#E0408F", "#E2D2F0", "#5FE4F0"
INK, MOUTH, GLINT = "#1E1430", "#2A1026", "#F4FFFF"

H = np.array([0.0, 0.0, 0.312])          # the head's middle
HR = np.array([0.082, 0.076, 0.1])
VOXEL = 0.0045


def bulb(V):
    """A sphere narrowed toward its top, so the head rises to the needle; a little fuller below."""
    V = V.copy()
    z = V[:, 2]
    s = 1 - 0.42 * np.clip(z, 0, 1) ** 1.6 + 0.06 * np.clip(-z, 0, 1)
    V[:, 0] *= s; V[:, 1] *= s
    return V


def needle(k, name, base, d, length, r, bone):
    """A pale needle: a round foot sunk in the head, a long taper, a blunt tip."""
    d = unit(*d)
    k.add(name, lathe(np.array(base) - d * 0.01, d, [(0, 0), (r, 0.0), (r * 0.95, length * 0.35), (r * 0.62, length * 0.8),
                                                    (r * 0.3, length * 0.97), (0.0, length)]),
          NEEDLE, bone, fuse=False, surface="shell")


def head(k):
    k.add("head", ell(H, HR, deform=bulb, sub=5), PURPLE, "head")
    # the lobes, a hood held round the face: rounded petals at the lower sides, a little forward
    for s in (1, -1):
        k.add("lobe" + ("R" if s > 0 else "L"), ell((0.08 * s, -0.022, 0.27), (0.036, 0.044, 0.068),
                                                    rot=(8, -22 * s, 0)), PURPLE, "head")
    # the chin under the mask, so the face has a jaw to sit on
    k.add("chin", ell((0, -0.03, 0.232), (0.05, 0.042, 0.035)), PURPLE, "head")
    needle(k, "needle", H + (0, 0.004, 0.092), (0, 0.06, 1), 0.075, 0.012, "head")
    for s in (1, -1):
        needle(k, "lobeneedle" + ("R" if s > 0 else "L"), (0.104 * s, -0.004, 0.315), (0.5 * s, 0.1, 1), 0.045, 0.009,
               "head")


def head_paint(k):
    # the crown: magenta round the needle's foot, its lower edge in points
    def crown(P):
        d = P - H
        th = np.arctan2(d[:, 0], -d[:, 1])
        edge = H[2] + 0.07 - 0.016 * np.clip(tri(th * 4 / (2 * math.pi)), 0, 1)
        return inside(edge - P[:, 2], 0.002) * inside(np.linalg.norm(d, axis=1) - 0.13)
    k.paint(crown, MAGENTA, on=["head"])
    # the lobes' upper outer corners
    for s in (1, -1):
        c = np.array([0.104 * s, -0.012, 0.31])
        k.paint(lambda P, c=c: inside(np.linalg.norm((P - c) / (0.035, 0.04, 0.03), axis=1) - 1, 0.08), MAGENTA,
                on=["lobe"])
    # the mask round the eyes, rising to a point between them, down to the chin
    def mask(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        top = 0.262 + 0.018 * np.clip(1 - np.abs(x) / 0.022, 0, 1) + 0.006 * np.clip(np.abs(x) - 0.03, 0, 1) / 0.03
        return inside(z - top, 0.002) * inside(np.abs(x) - 0.062, 0.003) * inside(y + 0.02, 0.006) \
            * inside(0.205 - z, 0.004)
    k.paint(mask, MAGENTA)


def brow_diamond(k):
    pts = [(0, 1), (0.5, 0), (0, -1), (-0.5, 0)]
    k.decal((0, -0.075, 0.322), unit(0, -1, 0.12), (0.022, 0.034), MAGENTA, eps=0.003, outline=polygon(pts),
            rings=10, segs=64)


def mouth(k):
    # a small smile: a thin crescent, one clean shape
    top = [(u, 0.2 + 0.55 * u * u) for u in np.linspace(1, -1, 13)]
    bot = [(u, -0.7 + 1.5 * u * u) for u in np.linspace(-0.92, 0.92, 11)]
    k.decal((0, -0.066, 0.229), unit(0, -1, -0.1), (0.017, 0.007), MOUTH, eps=0.003, outline=polygon(top + bot),
            rings=8, segs=72)


def body(k):
    k.add("neck", tube([(0, -0.008, 0.222), (0, -0.004, 0.19)], [0.018, 0.017]), MAGENTA, "body")
    k.add("trunk", ell((0, 0.0, 0.17), (0.034, 0.03, 0.038)), PURPLE, "body")
    # the arms: thin, forward and down from the trunk, small round hands
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        k.add("arm" + sd, tube([(0.026 * s, -0.01, 0.19), (0.044 * s, -0.035, 0.172), (0.04 * s, -0.058, 0.158)],
                               [0.01, 0.009, 0.0085]), PURPLE, "body")
        k.add("hand" + sd, ell((0.039 * s, -0.062, 0.155), (0.012, 0.011, 0.011)), PURPLE, "body")
    # the legs folded under: a round haunch, a short shin back down to a small pointed foot tipped magenta
    for s in (1, -1):
        sd = "R" if s > 0 else "L"
        bone = "leg" + sd
        k.add("haunch" + sd, ell((0.03 * s, -0.012, 0.135), (0.031, 0.036, 0.038), rot=(-20, 0, 8 * s)), PURPLE, bone)
        k.add("shin" + sd, tube([(0.035 * s, -0.035, 0.112), (0.04 * s, -0.02, 0.08), (0.04 * s, 0.01, 0.066)],
                                [0.015, 0.012, 0.009]), PURPLE, bone)
        k.add("foot" + sd, ell((0.04 * s, 0.016, 0.064), (0.01, 0.016, 0.009)), MAGENTA, bone)
    # the tail: down and back, curling out to +X, a band and a tip of magenta
    pts = [(0, 0.02, 0.15), (0.004, 0.05, 0.12), (0.022, 0.075, 0.085), (0.05, 0.08, 0.055), (0.075, 0.066, 0.038)]
    k.add("tail", tube(pts, [0.015, 0.013, 0.011, 0.0095, 0.008]), PURPLE, "tail")
    k.paint(lambda P: inside(0.07 - P[:, 0], 0.002) * inside(P[:, 2] - 0.06, 0.01) * inside(0.03 - P[:, 1], 0.4),
            MAGENTA, on=["tail"])
    k.paint(lambda P: inside(np.abs(P[:, 2] - 0.104) - 0.006, 0.002) * inside(0.05 - P[:, 1], 0.4), MAGENTA,
            on=["tail"])


# --- the eyes: angular cyan almonds, their outer corners swept up, a dark rim, a white glint -----------------------
EYE_OUT = [(-0.82, -0.25), (-0.45, -0.72), (0.35, -0.7), (0.9, -0.1), (1.0, 0.72), (0.45, 0.42), (-0.4, 0.38),
           (-0.82, 0.15)]
EYE_HALF = (0.032, 0.023)
RIM = 0.8              # the cyan's outline as a share of the dark outline under it
CC, CI, CG, CM = srgb(CYAN), srgb(INK), srgb(GLINT), srgb(MAGENTA)


def eye_open(u, v):
    c = np.tile(CC, (len(u), 1))
    c[((u + 0.25) / 0.24) ** 2 + ((v - 0.12) / 0.16) ** 2 < 1] = CG
    return c


def eye_frame(s):
    return np.array([0.035 * s, -0.062, 0.245]), unit(0.42 * s, -1, 0.05)


def eyes(k, state):
    look = eye_look(state, eye_open, CM, CI, line=0.18)
    for s in (1, -1):
        c, n = eye_frame(s)
        col = look if s > 0 else (lambda a, b, f=look: f(-a, b))
        outline = EYE_OUT if s > 0 else [(-u, v) for u, v in EYE_OUT]
        if state in ("Open", "Fierce"):
            # a dark outline under, the cyan a patch of its own over it: a crisp rim all round
            k.decal(c, n, EYE_HALF, INK, up=(0, 0, 1), eps=0.002, outline=polygon(outline), rings=12, segs=56)
            k.decal(c, n, (EYE_HALF[0] * RIM, EYE_HALF[1] * RIM), col, up=(0, 0, 1), eps=0.0032,
                    outline=polygon(outline), rings=20, segs=56)
        else:
            k.decal(c, n, EYE_HALF, col, up=(0, 0, 1), eps=0.002, outline=polygon(outline), rings=20, segs=56)


def poipole(src, out):
    k = Kit(src, coat="scales")
    head(k)
    head_paint(k)
    body(k)
    brow_diamond(k)
    mouth(k)
    k.build(voxel=VOXEL, smooth=5, band=0.006, wband=0.04)
    k.eye_states(lambda state: eyes(k, state))
    k.export(out)


if __name__ == "__main__":
    poipole(*args())
