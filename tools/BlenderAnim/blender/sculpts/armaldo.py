# Armaldo sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/armaldo.py -- <the export's Armaldo.glb> <out.glb>
# Grown from anorith.py (sibling("anorith")): the same white feathers with red tips as plates, the same dark face with
# its two red leaf marks, the same white eyes with a big black pupil, the same grey-black blades as hulls; the green
# turned to blue, the body stood up.
#
# From the references: an upright blue armoured beast leaning a little forward. A small head with a dark grey helmet
# over its top and snout, two red leaf marks on the helmet's front, a cream chin; at its sides two blue pods like ears,
# each with a white eye and a black pupil (the brief's "eyes" read from the pictures: the red marks are the helmet's,
# the eyes are in the pods). A long neck, cream down its front. A big cream plate over the chest and belly, narrowing
# to a point below, with a round black socket on each side where a blue peg runs out to the arm. Each arm a great blue
# blade of a forearm, pointed above the peg, curving down and forward to a black hooked claw. Three white feathers with
# red tips on each side of the neck's base. On the back two dark grey shells rimmed in yellow, folded like wings. Thick
# blue thighs, black round knee pads, short shins, feet with three white claws. A long blue tail out behind and to the
# side, two black spikes on it near its end.
# Skeleton: body (0, 0, 0.46), tail (0, 0.08, 0.34), arms (+-0.1, 0, 0.56), head (0, -0.03, 0.6),
# legs (+-0.08, 0.02, 0.28).
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, smoothstep, unit, eye_look, polygon, outward, hull, slab, lathe, \
    sibling, args

A = sibling("anorith")
FACE, RED, GREY, WHITE, INK = A.FACE, A.RED, A.GREY, A.WHITE, A.INK
BLUE, CREAM, RIM, DARK, CLAW = "#7AA4DA", "#EED890", "#E8CC6A", "#3C3E48", "#F2F2EE"

# --- the frame ---------------------------------------------------------------------------------------------------------
TORSO_C, TORSO_R = np.array([0.0, 0.03, 0.45]), (0.206, 0.162, 0.19)
# The head was laid out first about OLD_HC; it is now that layout grown by SC about HC, lower on a short thick neck
OLD_HC, HC, SC = np.array([0.0, -0.11, 0.895]), np.array([0.0, -0.13, 0.785]), 1.3
HEAD_R = (0.092 * SC, 0.115 * SC, 0.078 * SC)
NECK = [(0, 0.0, 0.6), (0, -0.05, 0.68), (0, -0.09, 0.74)]
SOCKET = np.array([0.112, -0.158, 0.49])          # where each arm's peg leaves the chest plate (+X side)
FORE = [np.array(p) for p in ((0.232, -0.205, 0.56), (0.24, -0.225, 0.49), (0.25, -0.275, 0.4), (0.24, -0.325, 0.33))]
POD_C_OLD = np.array([0.13, -0.085, 0.935])


def H(p):
    """A place of the head's first layout, where it is now."""
    return HC + (np.asarray(p, float) - OLD_HC) * SC


def plate_w(z):
    """The chest plate's half width at a height: broad at the shoulders, coming to a point below."""
    return np.clip(0.16 - 0.72 * np.clip(0.5 - z, 0, 1) ** 1.25, 0, 1) * (z < 0.64)


def body(k):
    k.add("torso", ell(TORSO_C, TORSO_R, rot=(10, 0, 0)), BLUE, "body")
    k.add("hips", ell((0, 0.04, 0.29), (0.19, 0.15, 0.11)), BLUE, "body")
    k.add("chest", ell((0, -0.065, 0.47), (0.17, 0.1, 0.16)), BLUE, "body")
    k.add("neck", tube(NECK, [0.13, 0.115, 0.098]), BLUE, "head")
    # the cream plate down the front, its point below, and the cream down the neck's front
    def plate(P):
        x, y, z = P[:, 0], P[:, 1], P[:, 2]
        return inside(np.abs(x) - plate_w(z), 0.003) * inside(y + 0.06, 0.006) * inside(0.24 - z, 0.004)
    k.paint(plate, CREAM, on=["torso", "hips", "chest"])
    k.paint(lambda P: inside(np.abs(P[:, 0]) - 0.055, 0.003) * inside(P[:, 1] - (-0.06 - 0.64 * (P[:, 2] - 0.62)), 0.004)
            * inside(P[:, 2] - 0.76, 0.004) * inside(0.6 - P[:, 2], 0.004), CREAM, on=["neck", "chest", "torso"])


def sockets(k):
    for s, side in ((1, "R"), (-1, "L")):
        c = SOCKET * (s, 1, 1)
        d = unit(s * 0.42, -1.0, 0.05)
        k.add(f"socket{side}", lathe(c - d * 0.006, d, [(0, 0), (0.042, 0), (0.042, 0.009), (0, 0.009)], seg=40),
              FACE, "body", fuse=False, surface="shell")


def arms(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        k.add(f"peg{side}", tube([SOCKET * m, (SOCKET + FORE[1]) / 2 * m, FORE[1] * m], [0.028, 0.027, 0.03]), BLUE,
              f"arm{side}")
        # the forearm: a great blue blade, blunt above the peg, swelling below it
        k.add(f"forearm{side}", tube([p * m for p in FORE], [0.042, 0.062, 0.072, 0.058]), BLUE, f"arm{side}")
        k.add(f"forebulk{side}", ell((0.245, -0.26, 0.43) * m, (0.069, 0.075, 0.1), rot=(25, 0, 0)), BLUE, f"arm{side}")
        # the black hooked claw, down and forward and in
        b = FORE[3]
        pts = [(b + o) * m for o in ((0.042, 0.024, 0.02), (-0.036, 0.024, 0.02), (0.042, -0.036, 0.025),
                                     (-0.036, -0.036, 0.025), (0.036, 0.0, -0.034), (-0.03, 0.0, -0.034),
                                     (0.0, -0.09, -0.065), (-0.024, -0.11, -0.11))]
        k.add(f"claw{side}", hull(pts), DARK, f"arm{side}", fuse=False, surface="shell")


def legs(k):
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        # huge thighs in a crouch, fused into the hips; the knee forward, a short thick shin back down to the foot
        k.add(f"thigh{side}", ell((0.135, -0.03, 0.225) * m, (0.118, 0.135, 0.13), rot=(-28, 0, 0)), BLUE,
              f"leg{side}")
        k.add(f"haunch{side}", ell((0.12, 0.04, 0.27) * m, (0.11, 0.11, 0.1)), BLUE, f"leg{side}")
        k.add(f"shin{side}", tube([(0.145, -0.1, 0.13) * m, (0.14, -0.072, 0.095) * m, (0.135, -0.06, 0.07) * m],
                                  [0.074, 0.07, 0.068]), BLUE, f"leg{side}")
        k.add(f"foot{side}", ell((0.135, -0.08, 0.04) * m, (0.08, 0.11, 0.04)), BLUE, f"leg{side}")
        k.add(f"knee{side}", outward(ell((0.15, -0.155, 0.15) * m, (0.06, 0.036, 0.06), sub=3)), DARK,
              f"leg{side}", fuse=False, surface="shell")
        for j, dx in enumerate((-0.045, 0.0, 0.045)):
            b = np.array([0.135 + dx, -0.18, 0.022])
            pts = [b + o for o in ((0.013, 0.0, 0.013), (-0.013, 0.0, 0.013), (0.013, 0.0, -0.013),
                                   (-0.013, 0.0, -0.013), (dx * 0.25, -0.04, -0.015))]
            k.add(f"toe{side}{j}", hull([p * m for p in pts]), CLAW, f"leg{side}", fuse=False, surface="shell")


def head(k):
    k.add("head", ell(HC, HEAD_R), BLUE, "head")
    k.add("snout", ell(H((0, -0.2, 0.875)), np.array((0.06, 0.06, 0.052)) * SC), BLUE, "head")
    k.add("chin", ell(H((0, -0.17, 0.845)), np.array((0.055, 0.06, 0.035)) * SC), CREAM, "head")
    # the dark helmet over the top and down the snout (the first layout's rule, read back through H)
    def helmet(P):
        Q = OLD_HC + (P - HC) / SC
        return inside(0.872 + 0.22 * (Q[:, 1] + 0.1) - Q[:, 2], 0.003 / SC) * inside(Q[:, 1] - 0.0, 0.01) \
            * (Q[:, 2] > 0.84) * inside(np.abs(Q[:, 0]) - 0.07, 0.003 / SC)
    k.paint(helmet, FACE)
    for s, side in ((1, "R"), (-1, "L")):
        m = np.array([s, 1, 1])
        k.add(f"pod{side}", ell(H(POD_C_OLD) * m, np.array((0.06, 0.04, 0.05)) * SC, rot=(0, s * -20, s * -25)), BLUE,
              "head")


def pod_eye():
    return H(POD_C_OLD + np.array([0.035, -0.032, 0.006])), (0.62, -0.78, 0.12)


def back(k):
    """Two dark shells folded on the back like wings, a yellow rim round each (a larger plate set behind)."""
    for s, side in ((1, "R"), (-1, "L")):
        top = np.array([0.09 * s, 0.17, 0.65])
        ex = unit(0.32 * s, 0.12, -1.0)            # down the back and out
        ez = unit(1.0 * s, 0.28, 0.32)             # across the shell, outward
        L, W = 0.46, 0.085
        shape = [(0.0, -0.01), (L * 0.15, -W * 0.7), (L * 0.55, -W), (L * 0.9, -W * 0.6), (L, 0.0), (L * 0.9, W * 0.5),
                 (L * 0.5, W * 0.85), (L * 0.15, W * 0.6)]
        n = np.cross(ex, ez); n /= np.linalg.norm(n)
        if n[1] < 0:
            n = -n
        k.add(f"shell{side}", slab(shape, top + n * 0.012, ex, ez, 0.014), DARK, "body", fuse=False, surface="shell")
        rim = [(a * 1.06 - 0.01, b * 1.18) for a, b in shape]
        k.add(f"shellrim{side}", slab(rim, top + n * 0.002, ex, ez, 0.012), RIM, "body", fuse=False, surface="shell")
        k.add(f"shellfront{side}", slab(shape, top - n * 0.008, ex, ez, 0.012), DARK, "body", fuse=False, surface="shell")


def fringe(k):
    for s, side in ((1, "R"), (-1, "L")):
        for i, (z, up) in enumerate(((0.72, 0.3), (0.67, 0.2), (0.62, 0.1))):
            root = (0.1 * s, 0.01, z)
            d = (s * 0.86, 0.42, up)
            A.feather(k, f"fringe{side}{i}", root, d, 0.17, 0.07, bone="head" if i == 0 else "body", roll=60.0, toward=-1.0)


def tail(k):
    pts = [(0.0, 0.11, 0.33), (0.04, 0.26, 0.22), (0.13, 0.4, 0.13), (0.24, 0.52, 0.09), (0.33, 0.6, 0.085)]
    k.add("tail", tube(pts, [0.085, 0.065, 0.045, 0.03, 0.016]), BLUE, "tail")
    for j, (p, L) in enumerate((((0.17, 0.445, 0.155), 0.11), ((0.26, 0.535, 0.11), 0.09))):
        A.prong(k, f"spike{j}", p, (0.25, 0.45, 1.0), L, 0.022, 0.016, bone="tail", colour=DARK)


def eyes(k, state):
    c, n = pod_eye()
    look = A.eye_open(pr=0.5, px=-0.18)
    A.ball_eyes(k, state, c, n, 0.03 * SC, lid=BLUE, look=look)


def armaldo(src, out):
    k = Kit(src, coat="shell")
    body(k)
    sockets(k)
    arms(k)
    legs(k)
    head(k)
    back(k)
    fringe(k)
    tail(k)
    A.red_marks(k, H((0.03, -0.205, 0.905)), (0.35, -0.75, 0.55), (0.02 * SC, 0.028 * SC))
    k.build(voxel=0.007, smooth=4, band=0.006)
    k.eye_states(lambda st: eyes(k, st))
    k.export(out)


if __name__ == "__main__":
    armaldo(*args())
