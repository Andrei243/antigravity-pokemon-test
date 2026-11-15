# Slowpoke sculpted from nothing on the skeleton and clips of the game's own export of it (sculptkit.py):
#   blender --background --factory-startup --python sculpts/slowpoke.py -- <the export's Slowpoke.glb> <out.glb>
#
# Written to be grown from (Slowbro: sibling("slowpoke")). From the references: a pink dopey creature lying low on its
# belly, its big round head and its body one soft mass; a broad flat cream muzzle like the top of a bill jutting out
# over a wide open mouth, rose inside, two small white fangs at its corners and two nostril dots on its top; big round
# white eyes, outlined dark, with tiny black pupils (a vacant stare), on the front of the head over the muzzle; small
# round ears on top with a curl inside; short front legs stretched forward under the chin, short hind legs, two white
# claws on each foot; a long thick tail rising behind, curling forward over the back, its tip paler.
import os, sys, math
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import numpy as np
from sculptkit import Kit, ell, tube, srgb, inside, unit, eye_look, outward, args

PINK, CREAM, MOUTH, CURL = "#F0A4B8", "#EADCA4", "#C25066", "#C9708A"
CLAW, TIP, NOSTRIL, INK = "#F4F2EE", "#F8E0E6", "#8C7656", "#16121A"

# The head's frame, as shares of its scale s (Slowpoke's head is s = 1)
HEAD_R = np.array([0.135, 0.122, 0.125])          # the head's half sizes
MUZZLE_AT, MUZZLE_R = np.array([0, -0.12, -0.022]), np.array([0.124, 0.085, 0.04])
JAW_AT, JAW_R = np.array([0, -0.08, -0.085]), np.array([0.1, 0.08, 0.06])
EYE_DIR = unit(0.46, -0.84, 0.36)                   # out from the head's middle to an eye
EYE_HALF = (0.031, 0.034)
EAR_AT = np.array([0.096, 0.015, 0.1])


def at_surface(C, R, d):
    """The point of the ellipsoid (C, R) along the direction d from its middle."""
    d = np.array(d, float)
    return C + d / np.linalg.norm(d / R)


def slow_head(k, HC, s, bone="head"):
    """
    The head of the line on bone, its middle at HC, scale s: head, muzzle, open mouth, fangs, nostrils, ears with their
    curls. Gives back where its eyes go, for eyes().
    """
    HC = np.array(HC, float)
    R = HEAD_R * s
    k.add("head", ell(HC, R), PINK, bone)
    # the cream muzzle, a broad flat bill tilted a little up at its front, over the open mouth
    MC = HC + MUZZLE_AT * s
    k.add("muzzle", ell(MC, MUZZLE_R * s, rot=(-8, 0, 0)), CREAM, bone)
    # the lower jaw, pink and round under the mouth
    k.add("jaw", ell(HC + JAW_AT * s, JAW_R * s), PINK, bone)
    # the open mouth carved between the muzzle and the jaw, aimed into the muzzle's overhang
    k.add("mouth", ell(HC + np.array([0, -0.155, -0.05]) * s, np.array([0.074, 0.05, 0.026]) * s, rot=(-6, 0, 0)),
          MOUTH, bone, carve=True)
    # two small white fangs hanging from the muzzle's underside at the mouth's corners
    for sx in (1, -1):
        root = HC + np.array([0.05 * sx, -0.168, -0.038]) * s
        k.add(f"fang{'RL'[sx < 0]}", outward(tube([root, root + np.array([0, -0.003, -0.018]) * s],
                                                  [0.009 * s, 0.002 * s], seg=12, per=3)),
              CLAW, bone, fuse=False, surface="shell")
    # the nostrils, two dots on the muzzle's top near its front
    for sx in (1, -1):
        p = MC + np.array([0.024 * sx, -0.055, 0.03]) * s
        k.decal(p, unit(0.15 * sx, -0.5, 1), (0.008 * s, 0.006 * s), NOSTRIL, eps=0.003, rings=6, segs=24)
    # small round ears on top, a curl drawn inside each
    for sx in (1, -1):
        ec = HC + EAR_AT * np.array([sx, 1, 1]) * s
        k.add(f"ear{'RL'[sx < 0]}", ell(ec, np.array([0.036, 0.026, 0.034]) * s, rot=(0, 25 * sx, 0)), PINK, bone)
        n = unit(0.45 * sx, -1, 0.25)
        def curl(u, v, sx=sx):
            uu = u * sx
            r = np.sqrt(uu * uu + v * v)
            th = (np.arctan2(v, uu) + math.pi) / (2 * math.pi)
            c = np.tile(srgb(PINK), (len(u), 1))
            line = (np.abs(r - (0.18 + 0.55 * th)) < 0.11) & (r < 0.86)
            c[line] = srgb(CURL)
            return c
        k.decal(ec + n * 0.03 * s, n, (0.022 * s, 0.022 * s), curl, eps=0.003, rings=12, segs=40)
    spots = []
    for sx in (1, -1):
        d = EYE_DIR * np.array([sx, 1, 1])
        spots.append((at_surface(HC, R, d), d, sx))
    return dict(spots=spots, half=(EYE_HALF[0] * s, EYE_HALF[1] * s), s=s)


WHITE = np.array([1.0, 1.0, 1.0])


def eye(u, v):
    """A round white eye with a dark outline (u outward, v up)."""
    c = np.tile(WHITE, (len(u), 1))
    c[u * u + v * v > 0.74] = srgb(INK)
    return c


def eyes(k, spec, state):
    """The eyes in one state: the white and its outline, and in the open states a tiny black pupil of its own."""
    look = eye_look(state, eye, srgb(PINK), srgb(INK), line=0.17)
    h = spec["half"]
    for at, d, sx in spec["spots"]:
        col = look if sx > 0 else (lambda u, v, look=look: look(-u, v))
        k.decal(at, d, h, col, eps=0.004, rings=20, segs=56)
        if state in ("Open", "Fierce"):
            right = np.cross((0, 0, 1.0), d); right /= np.linalg.norm(right)
            up = np.cross(d, right)
            # a tiny pupil, a little toward the nose and up: the vacant stare
            pc = at - right * 0.06 * h[0] + up * (0.05 if state == "Open" else -0.25) * h[1]
            ph = 0.27 * h[0]
            k.decal(pc, d, (ph, ph), INK, eps=0.007, rings=8, segs=32)


def claws(k, name, at, fwd, n, bone, size=1.0):
    """n white claws at the front of a foot at `at`, pointing along fwd."""
    fwd = unit(*fwd)
    side = np.cross(fwd, (0, 0, 1.0)); side /= np.linalg.norm(side)
    for i in range(n):
        off = (i - (n - 1) / 2) * 0.026 * size
        c = np.array(at, float) + side * off
        k.add(f"{name}{i}", ell(c, np.array([0.011, 0.014, 0.01]) * size, rot=(0, 0, math.degrees(math.atan2(fwd[0], -fwd[1])))),
              CLAW, bone, fuse=False, mirror=True, surface="shell")


def slowpoke(src, out):
    k = Kit(src, coat="scales")
    HC = np.array([0, -0.155, 0.215])
    spec = slow_head(k, HC, 1.25)
    # The body: a long soft loaf on its belly, joined to the head by a broad neck
    k.add("body", ell((0, 0.09, 0.15), (0.172, 0.205, 0.15)), PINK, "body")
    k.add("neck", ell((0, -0.05, 0.17), (0.16, 0.12, 0.15)), PINK, "body")
    # Front legs stretched forward under the chin, short and thick
    k.add("leg", tube([(0.1, -0.09, 0.11), (0.125, -0.2, 0.05), (0.13, -0.27, 0.035)], [0.05, 0.045, 0.04]),
          PINK, "leg{s}", mirror=True)
    k.add("paw", ell((0.13, -0.29, 0.03), (0.048, 0.052, 0.03)), PINK, "leg{s}", mirror=True)
    claws(k, "claw", (0.13, -0.337, 0.022), (0, -1, 0), 2, "leg{s}")
    # Hind legs: short stubs out to the side at the rump
    k.add("hind", tube([(0.1, 0.21, 0.11), (0.13, 0.2, 0.05), (0.14, 0.17, 0.03)], [0.055, 0.048, 0.04]),
          PINK, "hind{s}", mirror=True)
    k.add("hpaw", ell((0.14, 0.155, 0.028), (0.044, 0.05, 0.028)), PINK, "hind{s}", mirror=True)
    claws(k, "hclaw", (0.142, 0.108, 0.02), (0, -1, 0), 2, "hind{s}")
    # The tail: thick out of the rump, up behind and curling forward over the back, carried out to the +X side
    TAIL = [(0.01, 0.26, 0.17), (0.06, 0.35, 0.22), (0.12, 0.39, 0.31), (0.16, 0.37, 0.41), (0.175, 0.3, 0.47),
            (0.17, 0.215, 0.475)]
    k.add("tail", tube(TAIL, [0.088, 0.078, 0.066, 0.054, 0.042, 0.03]), PINK, "tail")
    tip = np.array(TAIL[-1])
    k.paint(lambda P: inside(np.linalg.norm(P - tip, axis=1) - 0.07, 0.02), TIP, on=["tail"])
    k.build(voxel=0.006, smooth=3, band=0.006)
    k.eye_states(lambda st: eyes(k, spec, st))
    k.export(out)


if __name__ == "__main__":
    slowpoke(*args())
