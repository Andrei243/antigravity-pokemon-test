# Cubchoo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/cubchoo.py -- <the export's Cubchoo.glb> <out.glb>
# Written to be grown from: beartic.py imports its frame (sibling("cubchoo")): the colours' way, the long snout with its
# big black nose, the round sole pads, the paw and the eye.
#
# The design, from the references: a small bear cub sitting; a great light blue head some three fifths of its height,
# wider than the body, with big round ears on the crown's corners; a long snout of a deeper periwinkle running forward
# and down from between the eyes to a big black nose; upright oval black eyes with a large white glint over a small
# one, set on the light blue beside the snout's root; a white round body, the head's blue reaching down over the
# shoulders behind; short white arms with the paws held together under the snout; short white legs sitting forward,
# dark grey round pads on the soles; a long blue drip of snot hanging from the nose to the ground, a teardrop at its end.
# Reference ratios (3D picture): head 0.6 of the height, head width / body width 1.15, ear radius / head width 0.23.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, boxy, eye_look, outward, lathe, args, sibling

t = sibling("teddiursa")
BLUE, SNOUT, WHITE, PAD = "#93D4F2", "#6E9DDB", "#F3F5FA", "#4A4C56"
NOSE, INK, SNOT, SNOTTIP = "#2C2A30", "#16141A", "#9CCDF0", "#D8EEFA"


def glint_eye(ink, big=(0.05, 0.36, 0.34), small=(-0.08, -0.46, 0.17)):
    """An upright black eye with a large white glint high and a small one low (u outward, v up)."""
    ink, white = np.array(ink, float), np.ones(3)
    def look(u, v):
        c = np.tile(ink, (len(u), 1))
        for x, y, r in (big, small):
            c[((u - x) / r) ** 2 + ((v - y) / r) ** 2 < 1] = white
        return c
    return look


def pad(k, c, n, half, color=PAD):
    """A round dark pad laid on a sole."""
    k.decal(c, n, half, color, eps=0.003, rings=10, segs=44, up=(0, 0, 1))


def cubchoo(src, out):
    k = Kit(src, coat="fur")
    H = np.array([0, -0.012, 0.245])           # the head's middle (its bone is at 0, -0.01, 0.16)
    HR = np.array([0.1, 0.088, 0.09])
    B = np.array([0, 0.004, 0.088])            # the body's middle

    # The body: a white ball, sitting, fullest low
    def pear(V):
        V = V.copy()
        s = 1 + 0.08 * np.clip(-V[:, 2], 0, 1) - 0.1 * np.clip(V[:, 2], 0, 1)
        V[:, 0] *= s; V[:, 1] *= s
        return V
    k.add("body", ell(B, (0.082, 0.076, 0.084), deform=pear), WHITE, "body")
    k.add("tail", ell((0, 0.078, 0.06), (0.022, 0.02, 0.02)), WHITE, "body")

    # The head: wide, a little squared; the blue runs down behind over the shoulders
    def head_shape(V):
        V = boxy(2.3)(V)
        V[:, 0] *= 1 + 0.04 * np.clip(-V[:, 2], 0, 1)
        return V
    HEAD = ell(H, HR, deform=head_shape)
    k.add("head", HEAD, BLUE, "head")
    k.add("nape", ell((0, 0.03, 0.17), (0.07, 0.05, 0.035)), BLUE, "head")

    # The snout: long, forward and a little down, a deeper periwinkle; a big black nose at its end
    SN = ell(H + (0, -0.085, -0.032), (0.047, 0.088, 0.048), rot=(-8, 0, 0))
    k.add("snout", SN, SNOUT, "head")
    NC = t.hit(SN, H + (0, -1, -0.02), (0, 1, 0))
    k.add("nose", outward(ell(NC + (0, 0.004, 0.002), (0.028, 0.016, 0.019), rot=(-10, 0, 0))), NOSE, "head",
          fuse=False, surface="shell")

    # Big round ears on the crown's corners, out to the sides
    k.add("ear", ell((0.098, 0.012, 0.31), (0.045, 0.022, 0.043), rot=(0, -25, 0)), BLUE, "ear{s}", mirror=True)

    # Short white arms, the paws together under the snout
    k.add("arm", tube([(0.05, -0.02, 0.135), (0.058, -0.06, 0.15), (0.034, -0.088, 0.166)], [0.026, 0.023, 0.021]),
          WHITE, "arm{s}", mirror=True)
    k.add("paw", ell((0.026, -0.098, 0.168), (0.027, 0.025, 0.03), rot=(0, 0, 15)), WHITE, "arm{s}", mirror=True)

    # Short white legs sitting forward, round soles with dark pads
    k.add("thigh", ell((0.05, -0.02, 0.045), (0.042, 0.05, 0.04)), WHITE, "leg{s}", mirror=True)
    FOOT = ell((0.062, -0.07, 0.032), (0.03, 0.026, 0.032))
    k.add("foot", FOOT, WHITE, "leg{s}", mirror=True)
    P = t.hit(FOOT, (0.062, -1, 0.034), (0, 1, 0))
    for s in (1, -1):
        pad(k, P * np.array([s, 1, 1]), unit(0.15 * s, -1, 0.1), (0.018, 0.019))

    # The drip of snot from the nose to the ground, a teardrop at its end
    d0 = NC + (0.006, -0.002, -0.006)
    # one teardrop of turning: a thin stream widening low into a round drop
    dd = unit(0.03, -0.08, -1)
    prof = [(0.0, 0.0)] + [(0.0075, s) for s in (0.004, 0.06)]
    prof += [(0.0075 + 0.0225 * ((s - 0.06) / 0.1) ** 1.3, s) for s in np.linspace(0.075, 0.16, 6)]
    prof += [(0.03 * math.cos(a), 0.16 + 0.03 * math.sin(a)) for a in np.radians(np.linspace(15, 75, 4))]
    prof += [(0.0, 0.19)]
    k.add("drip", lathe(d0, dd, prof, seg=32), SNOT, "head", fuse=False, surface="shell")
    k.add("dropglint", outward(ell(d0 + dd * 0.168 + (0.01, -0.024, 0), (0.011, 0.008, 0.013))), SNOTTIP, "head",
          fuse=False, surface="shell")

    # Eyes on the light blue beside the snout's root, upright ovals
    de = unit(0.5, -0.85, 0.1)
    E = t.hit(HEAD, (0.052, -1, H[2] + 0.022), (0, 1, 0))
    CB, CK = srgb(BLUE), srgb(INK)
    look = glint_eye(CK)
    def lay(state):
        k.decal(E, de, (0.016, 0.024), eye_look(state, look, CB, CK, line=0.2), eps=0.003, rings=20, segs=56,
                mirror=True)

    k.build(voxel=0.0036, band=0.004, wband=0.03)
    k.eye_states(lay)
    k.export(out)


if __name__ == "__main__":
    cubchoo(*args())
