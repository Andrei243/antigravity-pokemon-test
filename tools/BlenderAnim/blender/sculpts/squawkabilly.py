# Squawkabilly (green plumage) sculpted from nothing on the skeleton and clips of the game's own export of it:
#   blender --background --factory-startup --python sculpts/squawkabilly.py -- <the export's Squawkabilly.glb> <out.glb>
#
# The design, from the references: a stocky green parrot standing upright. A round green head (the face green too, not
# yellow) under a dark grey pompadour teased up like a cloud over its crown; a great yellow-orange hooked beak, open,
# a darker lower bill and a brownish mouth inside; heavy-lidded bored eyes, white with a black outline and pupil, a
# green lid over the top third; a white bib from under the beak down the chest to the belly; green wings, the left
# folded at its side with white flight feathers low behind, the right raised as if making a point, its long white
# flight feathers fanned (carried forward and up in front of the shoulder, not flung out sideways); a long green tail
# with white under it; short cream legs with three toes forward and one back.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, slab, srgb, inside, unit, eye_look, outward, lumpy, args

GREEN, DEEP, WHITE, CREST = "#62B848", "#4C9A38", "#F2F2EC", "#4A4A52"
BEAK, BEAKLOW, MOUTH, FOOT, CLAW, INK = "#F4B232", "#E08C26", "#9A4A44", "#F0D08C", "#D89232", "#1C1C20"

HC = np.array([0.0, -0.035, 0.352])
HR = np.array([0.087, 0.084, 0.086])


def feather(length, width):
    """A flight feather's outline along +a, its root at nought."""
    L, w = length, width
    return [(0, -w * 0.7), (L * 0.25, -w), (L * 0.7, -w * 0.8), (L * 0.95, -w * 0.25), (L, 0),
            (L * 0.95, w * 0.25), (L * 0.7, w * 0.8), (L * 0.25, w), (0, w * 0.7)]


def plate(k, name, root, d, n, length, width, colour, bone, thick=0.014):
    """A feather as a hard-edged plate from root along d, its face turned to n."""
    d = unit(*d); n = unit(*n)
    ez = np.cross(n, d); ez /= np.linalg.norm(ez)
    k.add(name, slab(feather(length, width), root, d, ez, thick), colour, bone, fuse=False, surface="fur")


def squawkabilly(src, out):
    k = Kit(src, coat="fur")

    # The body: a stocky egg, upright, the chest a little forward; a short thick neck into a round head
    k.add("body", ell((0, 0.012, 0.192), (0.102, 0.096, 0.132), rot=(-6, 0, 0)), GREEN, "body")
    k.add("neck", ell((0, -0.022, 0.29), (0.074, 0.07, 0.07)), GREEN, "head")
    k.add("head", ell(HC, HR), GREEN, "head")

    # The white bib: from under the beak and the lower cheeks down the chest to the belly
    def bib(P):
        a = ((P[:, 0] / 0.074) ** 2 + ((P[:, 1] + 0.11) / 0.1) ** 2 + ((P[:, 2] - 0.205) / 0.13) ** 2) - 1
        b = ((P[:, 0] / 0.07) ** 2 + ((P[:, 1] + 0.1) / 0.075) ** 2 + ((P[:, 2] - 0.31) / 0.05) ** 2) - 1
        return np.maximum(inside(a, 0.04), inside(b, 0.04)) * inside(P[:, 2] - 0.338, 0.003)
    k.paint(bib, WHITE, on=["body", "neck", "head"])

    # The pompadour: a dark cloud teased up over the crown, rolled forward over the brow
    k.add("crest", ell((0, -0.006, 0.45), (0.07, 0.064, 0.048), rot=(-12, 0, 0), deform=lumpy(5, 3, 0.1)), CREST, "head")
    k.add("crestroll", ell((0.006, -0.05, 0.445), (0.046, 0.034, 0.034), rot=(0, 0, 8)), CREST, "head")
    k.add("crestback", ell((0, 0.05, 0.415), (0.05, 0.034, 0.04)), CREST, "head")

    # The great beak: a hooked upper bill, open over a darker lower bill, the mouth brownish inside
    def hook(V):
        V = V.copy()
        t = np.clip(-V[:, 1], 0, 1)
        V[:, 0] *= 1 - 0.38 * t
        V[:, 2] -= 0.62 * t ** 2
        V[:, 2] *= 1 - 0.12 * t
        return V
    k.add("beak", ell((0, -0.112, 0.35), (0.047, 0.056, 0.045), deform=hook), BEAK, "head", fuse=False,
          surface="shell")
    k.add("mouth", ell((0, -0.105, 0.313), (0.032, 0.036, 0.016), rot=(-12, 0, 0)), MOUTH, "head", fuse=False,
          surface="shell")
    k.add("beaklow", ell((0, -0.108, 0.296), (0.036, 0.042, 0.017), rot=(-16, 0, 0)), BEAKLOW, "head", fuse=False,
          surface="shell")

    # The left wing folded at the side, white flight feathers low behind it
    k.add("wingL", ell((-0.094, 0.03, 0.215), (0.036, 0.092, 0.11), rot=(-18, 0, -4)), GREEN, "wingL")
    for i, (z, ln) in enumerate(((0.175, 0.13), (0.15, 0.115))):
        plate(k, f"primL{i}", (-0.103 - 0.006 * i, 0.07, z), (-0.12, 0.72, -0.62), (-1, 0.05, 0.1), ln, 0.026, WHITE,
              "wingL")
    k.add("covertL", ell((-0.104, 0.075, 0.17), (0.022, 0.05, 0.05), rot=(-40, 0, 0)), DEEP, "wingL")

    # The right wing raised as if to make a point: the arm up and forward, the white feathers fanned from the wrist
    sh, wr = np.array([0.08, 0.0, 0.27]), np.array([0.135, -0.075, 0.305])
    k.add("wingR", tube([sh, (0.115, -0.04, 0.29), wr], [0.042, 0.036, 0.03]), GREEN, "wingR")
    k.add("wristR", ell(wr + (0.004, 0.004, 0.0), (0.034, 0.03, 0.04), rot=(0, 30, 0)), GREEN, "wingR")
    out_ = unit(1, -0.45, 0)
    face = unit(0.25, -1, 0.15)
    for i, a in enumerate((-12, 12, 36, 60)):
        A = math.radians(a)
        d = out_ * math.cos(A) + np.array([0, 0, 1.0]) * math.sin(A)
        plate(k, f"primR{i}", wr + d * 0.012 + face * (-0.004 * i), d, face, 0.165 - 0.014 * abs(i - 1), 0.03, WHITE,
              "wingR")
    for i, a in enumerate((-6, 24, 50)):
        A = math.radians(a)
        d = out_ * math.cos(A) + np.array([0, 0, 1.0]) * math.sin(A)
        plate(k, f"covR{i}", wr + d * 0.008 + face * 0.006, d, face, 0.085, 0.03, GREEN, "wingR", thick=0.016)

    # The long tail: green feathers back and out to +X, banked so the cameras see a face, white under them
    d = unit(0.42, 0.8, -0.42)
    side = np.cross(d, (0, 0, 1.0)); side /= np.linalg.norm(side)
    up = np.cross(side, d)
    n = up * math.cos(math.radians(40)) + side * math.sin(math.radians(40))
    root = np.array([0.0, 0.085, 0.12])
    for i, (sw, ln) in enumerate(((-0.022, 0.17), (0.0, 0.2), (0.022, 0.17))):
        lat = np.cross(n, d)
        plate(k, f"tailw{i}", root + lat * sw - n * 0.011, d, n, ln * 0.93, 0.024, WHITE, "tail")
        plate(k, f"tail{i}", root + lat * sw, d, n, ln, 0.025, GREEN, "tail", thick=0.015)

    # Short cream legs, three toes forward and one back, each with an orange claw
    for sx in (1, -1):
        sd = "R" if sx > 0 else "L"
        top, ank = np.array([0.038 * sx, 0.0, 0.088]), np.array([0.042 * sx, -0.016, 0.034])
        k.add(f"leg{sd}", outward(tube([top, ank], [0.016, 0.013])), FOOT, f"leg{sd}", fuse=False, surface="shell")
        for j, (a, ln) in enumerate(((-26, 0.044), (0, 0.05), (26, 0.044), (180, 0.03))):
            r_ = math.radians(a)
            dd = unit(math.sin(r_) * sx, -math.cos(r_), -0.45)
            tip = ank + dd * ln
            k.add(f"toe{sd}{j}", outward(tube([ank, tip], [0.0095, 0.008])), FOOT, f"leg{sd}", fuse=False,
                  surface="shell")
            k.add(f"claw{sd}{j}", outward(tube([tip, tip + dd * 0.01 + np.array([0, 0, -0.004])], [0.0065, 0.0025])),
                  CLAW, f"leg{sd}", fuse=False, surface="shell")

    k.build(voxel=0.005, smooth=4, band=0.006)

    # The eyes: white with a black outline and pupil, a heavy green lid over the top third
    G, Wt, K = srgb(GREEN), srgb(WHITE), srgb(INK)
    def eye(u, v):
        c = np.tile(G, (len(u), 1))
        r = np.hypot(u, v)
        lid = 0.3 - 0.06 * u
        c[r < 1.0] = K
        c[(r < 0.8) & (v < lid - 0.1)] = Wt
        c[(((u + 0.02) / 0.3) ** 2 + ((v + 0.18) / 0.36) ** 2 < 1) & (v < lid - 0.1)] = K
        c[(r < 1.0) & (v > lid + 0.06)] = G
        return c
    de = unit(0.7, -0.56, 0.3)
    E = HC + de / np.linalg.norm(de / HR)
    def eyes(state):
        k.decal(E, de, (0.028, 0.03), eye_look(state, eye, G, K, line=0.16), eps=0.002, rings=20, segs=56,
                mirror=True)
    k.eye_states(eyes)
    k.export(out)


if __name__ == "__main__":
    squawkabilly(*args())
